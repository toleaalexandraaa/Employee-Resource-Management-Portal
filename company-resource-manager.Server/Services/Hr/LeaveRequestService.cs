using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Shared;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Hr;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly UserManager<Models.Identity.ApplicationUser> _userManager;

    public LeaveRequestService(
        AppDbContext db,
        INotificationService notifications,
        UserManager<Models.Identity.ApplicationUser> userManager)
    {
        _db = db;
        _notifications = notifications;
        _userManager = userManager;
    }

    public async Task<PagedResult<LeaveRequestDto>> GetAllAsync(Guid? employeeId, int page, int pageSize)
    {
        var query = _db.LeaveRequests.AsNoTracking().Include(r => r.Employee).AsQueryable();
        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.StartDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => ToDto(r))
            .ToListAsync();
        return new PagedResult<LeaveRequestDto>(items, total, page, pageSize);
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(Guid id)
    {
        var r = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);
        return r is null ? null : ToDto(r);
    }

    public async Task<LeaveRequestDto> SubmitAsync(Guid employeeId, CreateLeaveRequest request)
    {
        var leave = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            WorkingDaysCount = request.WorkingDaysCount,
            Reason = request.Reason,
            Status = LeaveStatus.Pending,
        };
        _db.LeaveRequests.Add(leave);
        await _db.SaveChangesAsync();

        // Notify all HrManagers and Admins
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);
        var hrManagers = await _userManager.GetUsersInRoleAsync("HrManager");
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        var recipients = (hrManagers ?? []).Union(admins ?? []).DistinctBy(u => u.Id);
        foreach (var manager in recipients)
        {
            await _notifications.CreateAsync(
                manager.Id,
                NotificationType.LeaveRequestSubmitted,
                "Leave Request Submitted",
                $"{employee?.FirstName} {employee?.LastName} submitted a {request.LeaveType} leave request.");
        }

        var loaded = await _db.LeaveRequests.AsNoTracking()
            .Include(x => x.Employee)
            .FirstAsync(x => x.Id == leave.Id);
        return ToDto(loaded);
    }

    public async Task<LeaveRequestDto?> ApproveAsync(Guid id, Guid reviewerUserId)
    {
        var leave = await _db.LeaveRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (leave is null || leave.Status != LeaveStatus.Pending) return null;

        leave.Status = LeaveStatus.Approved;
        leave.ReviewedBy = reviewerUserId;
        leave.ReviewedAt = DateTime.UtcNow;

        // Update leave balance
        var balance = await _db.LeaveBalances.FirstOrDefaultAsync(b =>
            b.EmployeeId == leave.EmployeeId &&
            b.Year == leave.StartDate.Year &&
            b.LeaveType == leave.LeaveType);

        if (balance is null)
        {
            balance = new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = leave.EmployeeId,
                Year = leave.StartDate.Year,
                LeaveType = leave.LeaveType,
                TotalDays = 21,
                UsedDays = 0,
            };
            _db.LeaveBalances.Add(balance);
        }
        balance.UsedDays += leave.WorkingDaysCount;

        await _db.SaveChangesAsync();

        // Notify employee
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == leave.EmployeeId);
        if (user is not null)
        {
            await _notifications.CreateAsync(
                user.Id,
                NotificationType.LeaveRequestApproved,
                "Leave Request Approved",
                $"Your {leave.LeaveType} leave request ({leave.StartDate:d} – {leave.EndDate:d}) has been approved.");
        }

        return ToDto(leave);
    }

    public async Task<LeaveRequestDto?> RejectAsync(Guid id, Guid reviewerUserId, string? reason)
    {
        var leave = await _db.LeaveRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (leave is null || leave.Status != LeaveStatus.Pending) return null;

        leave.Status = LeaveStatus.Rejected;
        leave.ReviewedBy = reviewerUserId;
        leave.ReviewedAt = DateTime.UtcNow;
        if (reason is not null) leave.Reason = reason;

        await _db.SaveChangesAsync();

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == leave.EmployeeId);
        if (user is not null)
        {
            await _notifications.CreateAsync(
                user.Id,
                NotificationType.LeaveRequestRejected,
                "Leave Request Rejected",
                $"Your {leave.LeaveType} leave request ({leave.StartDate:d} – {leave.EndDate:d}) has been rejected. {reason}");
        }

        return ToDto(leave);
    }

    public async Task<bool> CancelAsync(Guid id, Guid employeeId)
    {
        var leave = await _db.LeaveRequests.FirstOrDefaultAsync(r =>
            r.Id == id && r.EmployeeId == employeeId && r.Status == LeaveStatus.Pending);
        if (leave is null) return false;

        _db.LeaveRequests.Remove(leave);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetCalendarAsync(DateOnly from, DateOnly to)
    {
        return await _db.LeaveRequests.AsNoTracking()
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveStatus.Approved &&
                        r.StartDate <= to && r.EndDate >= from)
            .OrderBy(r => r.StartDate)
            .Select(r => ToDto(r))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetBalancesAsync(Guid employeeId, int year)
    {
        return await _db.LeaveBalances.AsNoTracking()
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .Select(b => new LeaveBalanceDto(
                b.Id, b.EmployeeId, b.Year, b.LeaveType,
                b.TotalDays, b.UsedDays, b.TotalDays - b.UsedDays))
            .ToListAsync();
    }

    private static LeaveRequestDto ToDto(LeaveRequest r) => new(
        r.Id, r.EmployeeId,
        r.Employee.FirstName + " " + r.Employee.LastName,
        r.LeaveType, r.StartDate, r.EndDate, r.WorkingDaysCount,
        r.Reason, r.Status, r.ReviewedBy, r.ReviewedAt);
}
