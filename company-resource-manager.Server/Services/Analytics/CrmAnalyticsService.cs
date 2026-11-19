using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Analytics;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Analytics;

public class CrmAnalyticsService : ICrmAnalyticsService
{
    private readonly AppDbContext _db;

    private static readonly LeadStage[] FunnelOrder =
        [LeadStage.New, LeadStage.Contacted, LeadStage.Qualified, LeadStage.ProposalSent, LeadStage.Won];

    public CrmAnalyticsService(AppDbContext db) => _db = db;

    // ── Leads ─────────────────────────────────────────────────────────────────

    public async Task<CrmLeadsDto> GetLeadsAsync()
    {
        var allLeads = await _db.Leads
            .Select(l => new { l.Stage, l.EstimatedValue, l.Source, l.CreatedAt, l.UpdatedAt })
            .ToListAsync();

        var funnel = FunnelOrder
            .Select(stage => new LeadFunnelStageDto(
                stage.ToString(),
                allLeads.Count(l => l.Stage == stage),
                allLeads.Where(l => l.Stage == stage).Sum(l => l.EstimatedValue ?? 0)))
            .ToList();

        int wonCount = allLeads.Count(l => l.Stage == LeadStage.Won);
        int lostCount = allLeads.Count(l => l.Stage == LeadStage.Lost);

        var leadsBySource = allLeads
            .GroupBy(l => l.Source.ToString())
            .Select(g => new LabelValueDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Value)
            .ToList();

        // Won leads with a known value
        var wonLeads = allLeads
            .Where(l => l.Stage == LeadStage.Won && l.EstimatedValue.HasValue)
            .ToList();

        var avgDealBySource = wonLeads
            .GroupBy(l => l.Source.ToString())
            .Select(g => new LabelValueDto(
                g.Key,
                Math.Round(g.Average(l => l.EstimatedValue!.Value), 0)))
            .OrderByDescending(x => x.Value)
            .ToList();

        var avgConversionDays = wonLeads
            .GroupBy(l => l.Source.ToString())
            .Select(g => new LabelValueDto(
                g.Key,
                Math.Round((decimal)g.Average(l =>
                    ((l.UpdatedAt ?? l.CreatedAt) - l.CreatedAt).TotalDays), 1)))
            .OrderBy(x => x.Label)
            .ToList();

        return new CrmLeadsDto(funnel, wonCount, lostCount, leadsBySource, avgDealBySource, avgConversionDays);
    }

    // ── Revenue ───────────────────────────────────────────────────────────────

    public async Task<CrmRevenueDto> GetRevenueAsync()
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Monthly revenue trend (last 12 months — paid invoices)
        var paidInvoices = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt != null)
            .Select(i => new { i.PaidAt, i.PaidAmount })
            .ToListAsync();

        var revenueSeries = Enumerable.Range(0, 12).Select(offset =>
        {
            var m = new DateTime(now.Year, now.Month, 1).AddMonths(-11 + offset);
            var total = paidInvoices
                .Where(i => i.PaidAt!.Value.Year == m.Year && i.PaidAt!.Value.Month == m.Month)
                .Sum(i => i.PaidAmount);
            return new MonthlyValueDto(m.ToString("MMM yyyy"), total);
        }).ToList();

        // Revenue by client (top 10)
        var revenueByClientRaw = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid)
            .GroupBy(i => i.Client.CompanyName)
            .Select(g => new { ClientName = g.Key, Total = g.Sum(i => i.PaidAmount) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToListAsync();
        var revenueByClient = revenueByClientRaw
            .Select(x => new LabelValueDto(x.ClientName, x.Total))
            .ToList();

        // Revenue by industry
        var revenueByIndustryRaw = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.Client.Industry != null)
            .GroupBy(i => i.Client.Industry!)
            .Select(g => new { Industry = g.Key, Total = g.Sum(i => i.PaidAmount) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();
        var revenueByIndustry = revenueByIndustryRaw
            .Select(x => new LabelValueDto(x.Industry, x.Total))
            .ToList();

        // Invoice aging — unpaid invoices (Sent, PartiallyPaid, Overdue)
        var unpaidRaw = await _db.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid
                     && i.Status != InvoiceStatus.Cancelled
                     && i.Status != InvoiceStatus.Draft)
            .Select(i => new { i.DueDate, Outstanding = i.TotalAmount - i.PaidAmount })
            .ToListAsync();

        var agingRaw = unpaidRaw
            .Select(i => new { AgeDays = today.DayNumber - i.DueDate.DayNumber, i.Outstanding })
            .ToList();

        var agingBuckets = new List<InvoiceAgingBucketDto>
        {
            new("Not Yet Due",
                agingRaw.Count(x => x.AgeDays < 0),
                agingRaw.Where(x => x.AgeDays < 0).Sum(x => x.Outstanding)),
            new("0–30 Days",
                agingRaw.Count(x => x.AgeDays >= 0 && x.AgeDays <= 30),
                agingRaw.Where(x => x.AgeDays >= 0 && x.AgeDays <= 30).Sum(x => x.Outstanding)),
            new("31–60 Days",
                agingRaw.Count(x => x.AgeDays >= 31 && x.AgeDays <= 60),
                agingRaw.Where(x => x.AgeDays >= 31 && x.AgeDays <= 60).Sum(x => x.Outstanding)),
            new("61–90 Days",
                agingRaw.Count(x => x.AgeDays >= 61 && x.AgeDays <= 90),
                agingRaw.Where(x => x.AgeDays >= 61 && x.AgeDays <= 90).Sum(x => x.Outstanding)),
            new("90+ Days",
                agingRaw.Count(x => x.AgeDays > 90),
                agingRaw.Where(x => x.AgeDays > 90).Sum(x => x.Outstanding)),
        };

        // Outstanding by status
        var outstandingRaw = await _db.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key.ToString(), Amount = g.Sum(i => i.TotalAmount - i.PaidAmount) })
            .ToListAsync();
        var outstandingByStatus = outstandingRaw
            .Select(x => new ReceivablesByStatusDto(x.Status, x.Amount))
            .ToList();
        var totalOutstanding = outstandingByStatus.Sum(x => x.Amount);

        return new CrmRevenueDto(
            revenueSeries, revenueByClient, revenueByIndustry,
            agingBuckets, totalOutstanding, outstandingByStatus);
    }

    // ── Clients ───────────────────────────────────────────────────────────────

    public async Task<CrmClientsDto> GetClientsAsync()
    {
        var now = DateTime.UtcNow;

        // By status
        var byStatusRaw = await _db.Clients
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        var clientsByStatus = byStatusRaw
            .Select(x => new LabelValueDto(x.Status, x.Count))
            .ToList();

        // By industry
        var byIndustryRaw = await _db.Clients
            .Where(c => c.Industry != null)
            .GroupBy(c => c.Industry!)
            .Select(g => new { Industry = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
        var clientsByIndustry = byIndustryRaw
            .Select(x => new LabelValueDto(x.Industry, x.Count))
            .ToList();

        // Acquisition by quarter (last 8 quarters)
        var twoYearsAgo = now.AddYears(-2);
        var acquisitionRaw = await _db.Clients
            .Where(c => c.CreatedAt >= twoYearsAgo)
            .Select(c => c.CreatedAt)
            .ToListAsync();

        var currQuarterStart = new DateTime(now.Year, ((now.Month - 1) / 3 * 3) + 1, 1);
        var acquisitionByQuarter = Enumerable.Range(0, 8).Select(i =>
        {
            var qStart = currQuarterStart.AddMonths(-3 * (7 - i));
            var qEnd = qStart.AddMonths(3);
            var count = acquisitionRaw.Count(d => d >= qStart && d < qEnd);
            var label = $"Q{(qStart.Month - 1) / 3 + 1} {qStart.Year}";
            return new MonthlyValueDto(label, count);
        }).ToList();

        // Top 10 clients by project count
        var topClientsRaw = await _db.Clients
            .Select(c => new
            {
                ClientName = c.CompanyName,
                ProjectCount = _db.Projects.Count(p => p.ClientId == c.Id),
                TotalInvoiced = c.Invoices.Sum(i => i.TotalAmount),
                TotalPaid = c.Invoices.Sum(i => i.PaidAmount),
            })
            .OrderByDescending(x => x.ProjectCount)
            .Take(10)
            .ToListAsync();
        var topClients = topClientsRaw
            .Select(x => new TopClientDto(x.ClientName, x.ProjectCount, x.TotalInvoiced, x.TotalPaid))
            .ToList();

        return new CrmClientsDto(clientsByStatus, clientsByIndustry, acquisitionByQuarter, topClients);
    }
}
