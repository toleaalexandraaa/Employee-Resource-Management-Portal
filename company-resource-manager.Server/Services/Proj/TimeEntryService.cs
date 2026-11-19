using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Proj;

public class TimeEntryService : ITimeEntryService
{
    private readonly AppDbContext _db;

    public TimeEntryService(AppDbContext db) => _db = db;

    public async Task<PagedResult<TimeEntryDto>> GetAllAsync(
        Guid? employeeId, Guid? projectId, DateOnly? from, DateOnly? to, int page, int pageSize)
    {
        var query = _db.TimeEntries.AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.Task)
            .AsQueryable();

        if (employeeId.HasValue) query = query.Where(e => e.EmployeeId == employeeId.Value);
        if (projectId.HasValue) query = query.Where(e => e.ProjectId == projectId.Value);
        if (from.HasValue) query = query.Where(e => e.Date >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Date <= to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.Date)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new PagedResult<TimeEntryDto>(await EnrichAsync(items), total, page, pageSize);
    }

    public async Task<TimeEntryDto> CreateAsync(CreateTimeEntryRequest r, Guid employeeId)
    {
        var isMember = await _db.ProjectMembers.AnyAsync(m =>
            m.ProjectId == r.ProjectId && m.EmployeeId == employeeId);
        if (!isMember)
            throw new InvalidOperationException("Employee is not a member of this project.");

        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            TaskId = r.TaskId,
            ProjectId = r.ProjectId,
            Date = r.Date,
            HoursLogged = r.HoursLogged,
            Description = r.Description,
        };
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();

        var loaded = await _db.TimeEntries.AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.Task)
            .FirstAsync(e => e.Id == entry.Id);
        return (await EnrichAsync([loaded])).First();
    }

    public async Task<TimeEntryDto?> UpdateAsync(Guid id, UpdateTimeEntryRequest r, Guid employeeId)
    {
        var entry = await _db.TimeEntries.FirstOrDefaultAsync(e => e.Id == id && e.EmployeeId == employeeId);
        if (entry is null) return null;

        entry.Date = r.Date;
        entry.HoursLogged = r.HoursLogged;
        entry.Description = r.Description;

        await _db.SaveChangesAsync();

        var loaded = await _db.TimeEntries.AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.Task)
            .FirstAsync(e => e.Id == id);
        return (await EnrichAsync([loaded])).First();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid employeeId)
    {
        var entry = await _db.TimeEntries.FirstOrDefaultAsync(e => e.Id == id && e.EmployeeId == employeeId);
        if (entry is null) return false;
        _db.TimeEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetProjectTotalHoursAsync(Guid projectId)
        => await _db.TimeEntries.Where(e => e.ProjectId == projectId).SumAsync(e => e.HoursLogged);

    private async Task<IReadOnlyList<TimeEntryDto>> EnrichAsync(IList<TimeEntry> entries)
    {
        var employeeIds = entries.Select(e => e.EmployeeId).Distinct().ToList();
        var employees = await _db.Employees.AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FirstName + " " + e.LastName);

        return entries.Select(e => new TimeEntryDto(
            e.Id, e.EmployeeId,
            employees.GetValueOrDefault(e.EmployeeId, "Unknown"),
            e.TaskId, e.Task?.Title,
            e.ProjectId, e.Project?.Name ?? string.Empty,
            e.Date, e.HoursLogged, e.Description)).ToList();
    }
}
