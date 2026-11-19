using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Analytics;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using TaskStatus = company_resource_manager.Server.Models.Proj.TaskStatus;

namespace company_resource_manager.Server.Services.Analytics;

public class ProjectAnalyticsService : IProjectAnalyticsService
{
    private readonly AppDbContext _db;

    public ProjectAnalyticsService(AppDbContext db) => _db = db;

    // ── Health ────────────────────────────────────────────────────────────────

    public async Task<ProjectHealthDto> GetHealthAsync()
    {
        // Status distribution
        var statusRaw = await _db.Projects
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var byStatus = statusRaw.Select(x => new LabelValueDto(x.Status.ToString(), x.Count)).ToList();

        // Budget variance — all projects, sorted by absolute variance descending
        var budgetRaw = await _db.Projects
            .Select(p => new { p.Name, Variance = p.ActualBudget - p.EstimatedBudget })
            .ToListAsync();
        var budgetVariances = budgetRaw
            .OrderByDescending(x => Math.Abs((double)x.Variance))
            .Select(x => new ProjectBudgetVarianceDto(x.Name, x.Variance))
            .ToList();

        // On-time delivery + duration scatter — completed projects with end date
        var completedRaw = await _db.Projects
            .Where(p => p.Status == ProjectStatus.Completed && p.EndDate != null)
            .Select(p => new { p.Name, p.StartDate, EndDate = p.EndDate!.Value, p.EstimatedDurationDays })
            .ToListAsync();

        var completedCount = completedRaw.Count;
        var lateProjectNames = completedRaw
            .Where(p => (p.EndDate.DayNumber - p.StartDate.DayNumber) > p.EstimatedDurationDays)
            .Select(p => p.Name)
            .ToList();
        var onTimeCount = completedCount - lateProjectNames.Count;

        var durationScatter = completedRaw
            .Select(p => new DurationScatterPointDto(
                p.Name,
                p.EstimatedDurationDays,
                p.EndDate.DayNumber - p.StartDate.DayNumber))
            .ToList();

        return new ProjectHealthDto(byStatus, budgetVariances, completedCount, onTimeCount, lateProjectNames, durationScatter);
    }

    // ── Time ──────────────────────────────────────────────────────────────────

    public async Task<ProjectTimeDto> GetTimeAsync()
    {
        var now = DateTime.UtcNow;
        var daysToMonday = ((int)now.DayOfWeek + 6) % 7;
        var weekStart = DateOnly.FromDateTime(now.AddDays(-daysToMonday));
        var today = DateOnly.FromDateTime(now);

        // Hours logged vs estimated by project
        var projectsRaw = await _db.Projects
            .Select(p => new
            {
                p.Name,
                EstimatedHours = p.Tasks.Sum(t => t.EstimatedHours),
                LoggedHours = p.TimeEntries.Sum(te => te.HoursLogged),
            })
            .ToListAsync();
        var hoursByProject = projectsRaw
            .OrderByDescending(x => x.LoggedHours)
            .Select(x => new ProjectHoursDto(x.Name, x.EstimatedHours, x.LoggedHours))
            .ToList();

        // Team utilization — active project members vs this week's hours
        var memberRaw = await _db.ProjectMembers
            .Where(pm => pm.LeaveDate == null || pm.LeaveDate > today)
            .Select(pm => new { pm.EmployeeId, pm.AllocatedHoursPerWeek })
            .ToListAsync();
        var allocatedByEmployee = memberRaw
            .GroupBy(pm => pm.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Sum(pm => pm.AllocatedHoursPerWeek));
        var employeeIds = allocatedByEmployee.Keys.ToList();

        var employeeNames = await _db.Employees
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, FullName = e.FirstName + " " + e.LastName })
            .ToListAsync();
        var nameDict = employeeNames.ToDictionary(e => e.Id, e => e.FullName);

        var weekEntries = await _db.TimeEntries
            .Where(te => te.Date >= weekStart && employeeIds.Contains(te.EmployeeId))
            .Select(te => new { te.EmployeeId, te.HoursLogged })
            .ToListAsync();
        var loggedByEmployee = weekEntries
            .GroupBy(te => te.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Sum(te => te.HoursLogged));

        var teamUtilization = allocatedByEmployee
            .Select(kvp => new MemberUtilizationDto(
                nameDict.GetValueOrDefault(kvp.Key, kvp.Key.ToString()),
                kvp.Value,
                loggedByEmployee.GetValueOrDefault(kvp.Key, 0m)))
            .OrderByDescending(m => m.AllocatedHoursPerWeek > 0
                ? m.LoggedHoursThisWeek / m.AllocatedHoursPerWeek
                : 0)
            .ToList();

        // Hours by day of week — last 12 months
        var twelveMonthsAgo = DateOnly.FromDateTime(now.AddMonths(-12));
        var allEntries = await _db.TimeEntries
            .Where(te => te.Date >= twelveMonthsAgo)
            .Select(te => new { te.Date, te.HoursLogged })
            .ToListAsync();

        string[] dayNames = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
        var hoursByDayOfWeek = Enumerable.Range(0, 7).Select(i =>
        {
            // i=0 → Monday (DayOfWeek=1), ..., i=6 → Sunday (DayOfWeek=0)
            var dow = (DayOfWeek)((i + 1) % 7);
            var total = allEntries
                .Where(te => te.Date.DayOfWeek == dow)
                .Sum(te => te.HoursLogged);
            return new LabelValueDto(dayNames[i], total);
        }).ToList();

        // Weekly hours — last 8 weeks
        var eightWeeksAgoStart = DateOnly.FromDateTime(now.AddDays(-daysToMonday - 49));
        var weeklyEntries = await _db.TimeEntries
            .Where(te => te.Date >= eightWeeksAgoStart)
            .Select(te => new { te.Date, te.HoursLogged })
            .ToListAsync();

        var weeklyHours = Enumerable.Range(0, 8).Select(i =>
        {
            var wStart = DateOnly.FromDateTime(now.AddDays(-daysToMonday - 7 * (7 - i)));
            var wEnd = wStart.AddDays(7);
            var total = weeklyEntries
                .Where(te => te.Date >= wStart && te.Date < wEnd)
                .Sum(te => te.HoursLogged);
            return new MonthlyValueDto(wStart.ToString("MMM d"), total);
        }).ToList();

        return new ProjectTimeDto(hoursByProject, teamUtilization, hoursByDayOfWeek, weeklyHours);
    }

    // ── Tasks ─────────────────────────────────────────────────────────────────

    public async Task<ProjectTasksDto> GetTasksAsync()
    {
        var now = DateTime.UtcNow;
        var daysToMonday = ((int)now.DayOfWeek + 6) % 7;

        // Task status distribution — all tasks
        var statusRaw = await _db.Tasks
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        var tasksByStatus = statusRaw.Select(x => new LabelValueDto(x.Status, x.Count)).ToList();

        // Tasks by priority — active projects only
        var priorityRaw = await _db.Tasks
            .Where(t => t.Project.Status == ProjectStatus.Active)
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        var tasksByPriority = priorityRaw.Select(x => new LabelValueDto(x.Priority, x.Count)).ToList();

        // Avg cycle time by priority — Done tasks, UpdatedAt - CreatedAt in days
        var doneTasks = await _db.Tasks
            .Where(t => t.Status == TaskStatus.Done)
            .Select(t => new { t.Priority, t.CreatedAt, t.UpdatedAt })
            .ToListAsync();
        var cycleTimeByPriority = doneTasks
            .GroupBy(t => t.Priority)
            .Select(g => new LabelValueDto(
                g.Key.ToString(),
                Math.Round((decimal)g.Average(t => ((t.UpdatedAt ?? t.CreatedAt) - t.CreatedAt).TotalDays), 1)))
            .OrderBy(x => x.Label)
            .ToList();

        // WIP violations — kanban columns where task count > WipLimit
        var wipRaw = await (
            from col in _db.KanbanColumns
            where col.WipLimit != null
            join board in _db.KanbanBoards on col.BoardId equals board.Id
            join project in _db.Projects on board.ProjectId equals project.Id
            select new
            {
                ProjectName = project.Name,
                ColumnName = col.Name,
                WipLimit = col.WipLimit!.Value,
                CurrentCount = col.Tasks.Count(),
            }
        ).Where(x => x.CurrentCount > x.WipLimit).ToListAsync();
        var wipViolations = wipRaw
            .Select(x => new WipViolationDto(x.ProjectName, x.ColumnName, x.WipLimit, x.CurrentCount))
            .ToList();

        // Task completion velocity — Done tasks updated in last 8 weeks, grouped by week
        var eightWeeksAgoStart = DateOnly.FromDateTime(now.AddDays(-daysToMonday - 49));
        var recentDone = await _db.Tasks
            .Where(t => t.Status == TaskStatus.Done && t.UpdatedAt >= eightWeeksAgoStart.ToDateTime(TimeOnly.MinValue))
            .Select(t => t.UpdatedAt)
            .ToListAsync();

        var completionVelocity = Enumerable.Range(0, 8).Select(i =>
        {
            var wStart = DateOnly.FromDateTime(now.AddDays(-daysToMonday - 7 * (7 - i)));
            var wEnd = wStart.AddDays(7);
            var count = recentDone.Count(d =>
                d >= wStart.ToDateTime(TimeOnly.MinValue) &&
                d < wEnd.ToDateTime(TimeOnly.MinValue));
            return new MonthlyValueDto(wStart.ToString("MMM d"), count);
        }).ToList();

        return new ProjectTasksDto(tasksByStatus, tasksByPriority, cycleTimeByPriority, wipViolations, completionVelocity);
    }
}
