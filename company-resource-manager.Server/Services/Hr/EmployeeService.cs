using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Hr;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmployeeService(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<PagedResult<EmployeeListItemDto>> GetAllAsync(int page, int pageSize)
    {
        var query = _db.Employees.AsNoTracking();
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new EmployeeListItemDto(
                e.Id, e.FirstName + " " + e.LastName,
                e.Department, e.JobTitle, e.WorkEmail, e.IsActive, e.SeniorityYears))
            .ToListAsync();
        return new PagedResult<EmployeeListItemDto>(items, total, page, pageSize);
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Employees.AsNoTracking()
            .Include(x => x.Manager)
            .FirstOrDefaultAsync(x => x.Id == id);
        return e is null ? null : ToDto(e);
    }

    public async Task<CreateEmployeeResult> CreateAsync(CreateEmployeeRequest r)
    {
        var employee = new Employee
        {
            FirstName = r.FirstName,
            LastName = r.LastName,
            PersonalEmail = r.PersonalEmail,
            WorkEmail = r.WorkEmail,
            Phone = r.Phone,
            BirthDate = r.BirthDate,
            HireDate = r.HireDate,
            Department = r.Department,
            JobTitle = r.JobTitle,
            ManagerId = r.ManagerId,
            SeniorityYears = r.SeniorityYears,
            IsActive = true,
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        var tempPassword = GenerateTempPassword();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = r.WorkEmail,
            Email = r.WorkEmail,
            EmailConfirmed = true,
            FullName = $"{r.FirstName} {r.LastName}",
            IsActive = true,
            EmployeeId = employee.Id,
        };
        var result = await _userManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to create login for {r.WorkEmail}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        await _userManager.AddToRoleAsync(user, r.Role);

        var dto = await GetByIdAsync(employee.Id) ?? ToDto(employee);
        return new CreateEmployeeResult(dto, tempPassword);
    }

    private static string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        var sb = new char[10];

        for (int i = 0; i < sb.Length; i++)
            sb[i] = chars[Random.Shared.Next(chars.Length)];

        return "Temp@1" + new string(sb);
    }

    public async Task<EmployeeDto?> UpdateAsync(Guid id, UpdateEmployeeRequest r)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return null;

        employee.FirstName = r.FirstName;
        employee.LastName = r.LastName;
        employee.PersonalEmail = r.PersonalEmail;
        employee.WorkEmail = r.WorkEmail;
        employee.Phone = r.Phone;
        employee.Department = r.Department;
        employee.JobTitle = r.JobTitle;
        employee.ManagerId = r.ManagerId;
        employee.SeniorityYears = r.SeniorityYears;
        employee.IsActive = r.IsActive;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return false;
        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
        return true;
    }

    private static EmployeeDto ToDto(Employee e) => new(
        e.Id, e.FirstName, e.LastName, e.FirstName + " " + e.LastName,
        e.PersonalEmail, e.WorkEmail, e.Phone,
        e.BirthDate, e.HireDate, e.Department, e.JobTitle,
        e.ManagerId, e.Manager is null ? null : e.Manager.FirstName + " " + e.Manager.LastName,
        e.SeniorityYears, e.IsActive);
}
