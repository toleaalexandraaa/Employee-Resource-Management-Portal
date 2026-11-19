using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Shared;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Hr;

public class PayrollService : IPayrollService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly UserManager<Models.Identity.ApplicationUser> _userManager;

    public PayrollService(
        AppDbContext db,
        INotificationService notifications,
        UserManager<Models.Identity.ApplicationUser> userManager)
    {
        _db = db;
        _notifications = notifications;
        _userManager = userManager;
    }

    public async Task<PagedResult<PayrollRecordDto>> GetAllAsync(int page, int pageSize)
    {
        var query = _db.PayrollRecords.AsNoTracking().Include(p => p.Employee);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync();
        return new PagedResult<PayrollRecordDto>(items, total, page, pageSize);
    }

    public async Task<PayrollRecordDto?> GetByIdAsync(Guid id)
    {
        var p = await _db.PayrollRecords.AsNoTracking()
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? null : ToDto(p);
    }

    public async Task<PayrollRecordDto> GenerateAsync(GeneratePayrollRequest r)
    {
        var duplicate = await _db.PayrollRecords.AnyAsync(p =>
            p.EmployeeId == r.EmployeeId && p.Month == r.Month && p.Year == r.Year);
        if (duplicate)
            throw new InvalidOperationException($"Payroll already generated for employee {r.EmployeeId} - {r.Month}/{r.Year}.");

        var contract = await _db.EmploymentContracts
            .AsNoTracking()
            .Where(c => c.EmployeeId == r.EmployeeId && c.IsActive)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No active employment contract found for this employee.");

        var grossSalary = contract.GrossSalary;
        var taxDeductions = grossSalary * 0.10m;
        var socialContributions = grossSalary * 0.35m;
        var netSalary = grossSalary + r.BonusAmount - taxDeductions - socialContributions
                        + (r.MealTicketCount * r.MealTicketValue);

        var record = new PayrollRecord
        {
            EmployeeId = r.EmployeeId,
            Month = r.Month,
            Year = r.Year,
            GrossSalary = grossSalary,
            BonusAmount = r.BonusAmount,
            MealTicketCount = r.MealTicketCount,
            MealTicketValue = r.MealTicketValue,
            TaxDeductions = taxDeductions,
            SocialContributions = socialContributions,
            NetSalary = netSalary,
            GeneratedAt = DateTime.UtcNow,
            Status = PayrollStatus.Draft,
        };

        _db.PayrollRecords.Add(record);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(record.Id) ?? throw new InvalidOperationException("Failed to load generated record.");
    }

    public async Task<PayrollRecordDto?> ApproveAsync(Guid id, Guid approverUserId)
    {
        var record = await _db.PayrollRecords
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (record is null || record.Status != PayrollStatus.Draft) return null;

        record.Status = PayrollStatus.Approved;
        await _db.SaveChangesAsync();

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == record.EmployeeId);
        if (user is not null)
        {
            await _notifications.CreateAsync(
                user.Id,
                NotificationType.PayrollApproved,
                "Payroll Approved",
                $"Your payroll for {record.Month}/{record.Year} has been approved. Net: {record.NetSalary:C} {record.Employee.Department}.");
        }

        return ToDto(record);
    }

    public async Task<PayrollRecordDto?> MarkAsPaidAsync(Guid id)
    {
        var record = await _db.PayrollRecords
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (record is null || record.Status != PayrollStatus.Approved) return null;

        record.Status = PayrollStatus.Paid;
        await _db.SaveChangesAsync();

        return ToDto(record);
    }

    private static PayrollRecordDto ToDto(PayrollRecord p) => new(
        p.Id, p.EmployeeId,
        p.Employee.FirstName + " " + p.Employee.LastName,
        p.Month, p.Year, p.GrossSalary, p.BonusAmount,
        p.MealTicketCount, p.MealTicketValue,
        p.TaxDeductions, p.SocialContributions, p.NetSalary,
        p.GeneratedAt, p.Status);
}
