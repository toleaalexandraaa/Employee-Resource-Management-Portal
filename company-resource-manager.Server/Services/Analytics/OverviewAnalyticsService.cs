using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Analytics;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Analytics;

public class OverviewAnalyticsService : IOverviewAnalyticsService
{
    private readonly AppDbContext _db;

    public OverviewAnalyticsService(AppDbContext db) => _db = db;

    public async Task<OverviewDto> GetOverviewAsync()
    {
        var now = DateTime.UtcNow;

        // ── KPI queries (sequential — DbContext is not thread-safe) ──────────
        var activeEmployees = await _db.Employees
            .CountAsync(e => e.IsActive);

        var payrollThisMonth = await _db.PayrollRecords
            .Where(p => p.Month == now.Month && p.Year == now.Year && p.Status == PayrollStatus.Paid)
            .SumAsync(p => (decimal?)p.NetSalary) ?? 0m;

        var activeProjects = await _db.Projects
            .CountAsync(p => p.Status == ProjectStatus.Active);

        var budgetProjects = await _db.Projects
            .Where(p => p.EstimatedBudget > 0 && p.ActualBudget > 0)
            .Select(p => new { p.EstimatedBudget, p.ActualBudget })
            .ToListAsync();

        var openLeads = await _db.Leads
            .Where(l => l.Stage != LeadStage.Won && l.Stage != LeadStage.Lost)
            .Select(l => l.EstimatedValue)
            .ToListAsync();

        var revenueThisMonth = await _db.Invoices
            .Where(i => i.PaidAt.HasValue && i.PaidAt.Value.Month == now.Month && i.PaidAt.Value.Year == now.Year)
            .SumAsync(i => (decimal?)i.PaidAmount) ?? 0m;

        var overdueAmounts = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Overdue)
            .Select(i => i.TotalAmount - i.PaidAmount)
            .ToListAsync();

        var pendingLeave = await _db.LeaveRequests
            .CountAsync(l => l.Status == LeaveStatus.Pending);

        // ── Chart data queries ────────────────────────────────────────────────
        var revenueStartDate = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
        var revenueRaw = await _db.Invoices
            .Where(i => i.PaidAt.HasValue && i.PaidAt.Value >= revenueStartDate)
            .Select(i => new { i.PaidAt, i.PaidAmount })
            .ToListAsync();

        var hiresStartDate = DateOnly.FromDateTime(new DateTime(now.Year, now.Month, 1).AddMonths(-5));
        var hiresRaw = await _db.Employees
            .Where(e => e.HireDate >= hiresStartDate)
            .Select(e => e.HireDate)
            .ToListAsync();

        // ── Compute budget variance ───────────────────────────────────────────
        double budgetVariancePct = 0;
        if (budgetProjects.Count > 0)
        {
            var totalEstimated = budgetProjects.Sum(p => p.EstimatedBudget);
            var totalActual = budgetProjects.Sum(p => p.ActualBudget);
            budgetVariancePct = totalEstimated > 0
                ? (double)((totalActual - totalEstimated) / totalEstimated * 100)
                : 0;
        }

        // ── Build revenue chart (last 12 months) ─────────────────────────────
        var revenueByMonth = revenueRaw
            .GroupBy(i => new { i.PaidAt!.Value.Year, i.PaidAt!.Value.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.Sum(i => i.PaidAmount));

        var revenueSeries = Enumerable.Range(0, 12)
            .Select(offset =>
            {
                var monthDate = new DateTime(now.Year, now.Month, 1).AddMonths(-11 + offset);
                var label = monthDate.ToString("MMM yyyy");
                var value = revenueByMonth.TryGetValue((monthDate.Year, monthDate.Month), out var v) ? v : 0m;
                return new MonthlyValueDto(label, value);
            })
            .ToList();

        // ── Build hires chart (last 6 months) ────────────────────────────────
        var hiresByMonth = hiresRaw
            .GroupBy(d => new { d.Year, d.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => (decimal)g.Count());

        var hiresSeries = Enumerable.Range(0, 6)
            .Select(offset =>
            {
                var monthDate = new DateTime(now.Year, now.Month, 1).AddMonths(-5 + offset);
                var label = monthDate.ToString("MMM yyyy");
                var value = hiresByMonth.TryGetValue((monthDate.Year, monthDate.Month), out var v) ? v : 0m;
                return new MonthlyValueDto(label, value);
            })
            .ToList();

        return new OverviewDto(
            ActiveEmployees: activeEmployees,
            PayrollThisMonth: payrollThisMonth,
            ActiveProjects: activeProjects,
            BudgetVariancePct: Math.Round(budgetVariancePct, 1),
            OpenLeadsCount: openLeads.Count,
            OpenLeadsValue: openLeads.Sum(v => v ?? 0m),
            RevenueThisMonth: revenueThisMonth,
            OverdueInvoicesCount: overdueAmounts.Count,
            OverdueInvoicesAmount: overdueAmounts.Sum(),
            PendingLeaveRequests: pendingLeave,
            RevenueLast12Months: revenueSeries,
            HiresLast6Months: hiresSeries
        );
    }
}
