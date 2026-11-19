using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Analytics;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Analytics;

public class HrAnalyticsService : IHrAnalyticsService
{
    private readonly AppDbContext _db;

    private static readonly LeaveType[] LeaveTypeOrder =
        [LeaveType.Annual, LeaveType.Medical, LeaveType.Unpaid, LeaveType.Parental, LeaveType.Other];

    public HrAnalyticsService(AppDbContext db) => _db = db;

    // ── Workforce ─────────────────────────────────────────────────────────────

    public async Task<WorkforceDto> GetWorkforceAsync()
    {
        var now = DateTime.UtcNow;

        var byDept = await _db.Employees
            .Where(e => e.IsActive)
            .GroupBy(e => e.Department)
            .Select(g => new { Dept = g.Key, Count = (decimal)g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var byContractType = await _db.EmploymentContracts
            .Where(c => c.IsActive && c.Employee.IsActive)
            .GroupBy(c => c.ContractType)
            .Select(g => new { Type = g.Key.ToString(), Count = (decimal)g.Count() })
            .ToListAsync();

        var seniorityRaw = await _db.Employees
            .Where(e => e.IsActive)
            .Select(e => e.SeniorityYears)
            .ToListAsync();

        var seniorityBuckets = new[]
        {
            new LabelValueDto("0-2 yrs",  seniorityRaw.Count(s => s < 2)),
            new LabelValueDto("2-5 yrs",  seniorityRaw.Count(s => s >= 2 && s < 5)),
            new LabelValueDto("5-10 yrs", seniorityRaw.Count(s => s >= 5 && s < 10)),
            new LabelValueDto("10+ yrs",  seniorityRaw.Count(s => s >= 10)),
        };

        var hiresStart = DateOnly.FromDateTime(new DateTime(now.Year, now.Month, 1).AddMonths(-23));
        var hiresRaw = await _db.Employees
            .Where(e => e.HireDate >= hiresStart)
            .Select(e => e.HireDate)
            .ToListAsync();

        var hireSeries = Enumerable.Range(0, 24).Select(offset =>
        {
            var m = new DateTime(now.Year, now.Month, 1).AddMonths(-23 + offset);
            return new MonthlyValueDto(m.ToString("MMM yyyy"),
                hiresRaw.Count(d => d.Year == m.Year && d.Month == m.Month));
        }).ToList();

        return new WorkforceDto(
            byDept.Select(x => new LabelValueDto(x.Dept, x.Count)).ToList(),
            byContractType.Select(x => new LabelValueDto(x.Type, x.Count)).ToList(),
            seniorityBuckets,
            hireSeries
        );
    }

    // ── Leave ─────────────────────────────────────────────────────────────────

    public async Task<HrLeaveDto> GetLeaveAsync()
    {
        var now = DateTime.UtcNow;

        // Balance utilization (current year, aggregated by leave type)
        var balancesRaw = await _db.LeaveBalances
            .Where(b => b.Year == now.Year)
            .GroupBy(b => b.LeaveType)
            .Select(g => new { LeaveType = g.Key, Total = g.Sum(b => b.TotalDays), Used = g.Sum(b => b.UsedDays) })
            .ToListAsync();

        var balanceUtilization = balancesRaw
            .Select(b => new LeaveBalanceItemDto(b.LeaveType.ToString(), b.Total, b.Used))
            .ToList();

        // Leave by month (last 12 months, stacked by type)
        var twelveMonthsAgo = DateOnly.FromDateTime(new DateTime(now.Year, now.Month, 1).AddMonths(-11));
        var approvedLeave = await _db.LeaveRequests
            .Where(l => l.Status == LeaveStatus.Approved && l.StartDate >= twelveMonthsAgo)
            .Select(l => new { l.StartDate.Month, l.StartDate.Year, l.LeaveType, l.WorkingDaysCount })
            .ToListAsync();

        var leaveMonths = Enumerable.Range(0, 12)
            .Select(offset => new DateTime(now.Year, now.Month, 1).AddMonths(-11 + offset))
            .ToList();

        var leaveMonthLabels = leaveMonths.Select(m => m.ToString("MMM yyyy")).ToList();
        var leaveTypeLabels = LeaveTypeOrder.Select(t => t.ToString()).ToList();

        var leaveByMonthMatrix = LeaveTypeOrder
            .Select(type => (IReadOnlyList<decimal>)leaveMonths
                .Select(m => (decimal)approvedLeave
                    .Where(l => l.LeaveType == type && l.Month == m.Month && l.Year == m.Year)
                    .Sum(l => l.WorkingDaysCount))
                .ToList())
            .ToList();

        // Pending requests (oldest first, max 30)
        var pendingRequests = await _db.LeaveRequests
            .Where(l => l.Status == LeaveStatus.Pending)
            .OrderBy(l => l.CreatedAt)
            .Take(30)
            .Select(l => new PendingLeaveItemDto(
                l.Id,
                l.Employee.FirstName + " " + l.Employee.LastName,
                l.LeaveType.ToString(),
                l.StartDate,
                l.EndDate,
                l.WorkingDaysCount,
                l.CreatedAt))
            .ToListAsync();

        // Leave by department heatmap (last 6 months)
        var sixMonthsAgo = DateOnly.FromDateTime(new DateTime(now.Year, now.Month, 1).AddMonths(-5));
        var heatmapMonths = Enumerable.Range(0, 6)
            .Select(offset => new DateTime(now.Year, now.Month, 1).AddMonths(-5 + offset))
            .ToList();

        var heatmapMonthLabels = heatmapMonths.Select(m => m.ToString("MMM yyyy")).ToList();

        var leaveForHeatmap = await _db.LeaveRequests
            .Where(l => l.Status == LeaveStatus.Approved && l.StartDate >= sixMonthsAgo)
            .Select(l => new { l.Employee.Department, l.StartDate.Month, l.StartDate.Year, l.WorkingDaysCount })
            .ToListAsync();

        var allDepts = await _db.Employees
            .Where(e => e.IsActive)
            .Select(e => e.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        var leaveByDepartment = allDepts.Select(dept =>
            new DepartmentHeatmapRowDto(dept,
                heatmapMonths.Select(m => (decimal)leaveForHeatmap
                    .Where(l => l.Department == dept && l.Month == m.Month && l.Year == m.Year)
                    .Sum(l => l.WorkingDaysCount))
                .ToList()))
            .ToList();

        return new HrLeaveDto(
            balanceUtilization,
            leaveMonthLabels,
            leaveTypeLabels,
            leaveByMonthMatrix,
            pendingRequests.Count,
            pendingRequests,
            heatmapMonthLabels,
            leaveByDepartment
        );
    }

    // ── Payroll ───────────────────────────────────────────────────────────────

    public async Task<HrPayrollDto> GetPayrollAsync()
    {
        var now = DateTime.UtcNow;

        // Cost trend — last 12 months (paid records)
        var costRaw = await _db.PayrollRecords
            .Where(p => p.Status == PayrollStatus.Paid)
            .Select(p => new { p.Month, p.Year, p.NetSalary })
            .ToListAsync();

        var costSeries = Enumerable.Range(0, 12).Select(offset =>
        {
            var m = new DateTime(now.Year, now.Month, 1).AddMonths(-11 + offset);
            var total = costRaw.Where(p => p.Year == m.Year && p.Month == m.Month).Sum(p => p.NetSalary);
            return new MonthlyValueDto(m.ToString("MMM yyyy"), total);
        }).ToList();

        // Average gross salary by department (active contracts + active employees)
        var salaryRaw = await _db.EmploymentContracts
            .Where(c => c.IsActive && c.Employee.IsActive)
            .Select(c => new { c.Employee.Department, c.GrossSalary })
            .ToListAsync();

        var avgSalaryByDept = salaryRaw
            .GroupBy(x => x.Department)
            .Select(g => new LabelValueDto(g.Key, g.Average(x => x.GrossSalary)))
            .OrderByDescending(x => x.Value)
            .ToList();

        // Composition — average components from current month (Approved + Paid)
        var compRaw = await _db.PayrollRecords
            .Where(p => p.Month == now.Month && p.Year == now.Year && p.Status != PayrollStatus.Draft)
            .Select(p => new
            {
                p.GrossSalary, p.BonusAmount,
                MealTickets = p.MealTicketCount * p.MealTicketValue,
                p.TaxDeductions, p.SocialContributions, p.NetSalary,
            })
            .ToListAsync();

        var composition = compRaw.Count > 0
            ? new PayrollCompositionDto(
                compRaw.Average(p => p.GrossSalary),
                compRaw.Average(p => p.BonusAmount),
                compRaw.Average(p => p.MealTickets),
                compRaw.Average(p => p.TaxDeductions),
                compRaw.Average(p => p.SocialContributions),
                compRaw.Average(p => p.NetSalary))
            : new PayrollCompositionDto(0, 0, 0, 0, 0, 0);

        // Status counts for current month
        var statusRaw = await _db.PayrollRecords
            .Where(p => p.Month == now.Month && p.Year == now.Year)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int draft = statusRaw.FirstOrDefault(x => x.Status == PayrollStatus.Draft)?.Count ?? 0;
        int approved = statusRaw.FirstOrDefault(x => x.Status == PayrollStatus.Approved)?.Count ?? 0;
        int paid = statusRaw.FirstOrDefault(x => x.Status == PayrollStatus.Paid)?.Count ?? 0;

        return new HrPayrollDto(costSeries, avgSalaryByDept, composition, draft, approved, paid);
    }
}
