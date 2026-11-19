using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Hr;

public class EmploymentContractService : IEmploymentContractService
{
    private readonly AppDbContext _db;

    public EmploymentContractService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EmploymentContractDto>> GetByEmployeeAsync(Guid employeeId)
    {
        return await _db.EmploymentContracts.AsNoTracking()
            .Include(c => c.Employee)
            .Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.StartDate)
            .Select(c => ToDto(c))
            .ToListAsync();
    }

    public async Task<EmploymentContractDto> CreateAsync(CreateContractRequest r)
    {
        var contract = new EmploymentContract
        {
            EmployeeId = r.EmployeeId,
            ContractType = r.ContractType,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            GrossSalary = r.GrossSalary,
            Currency = r.Currency,
            IsActive = true,
        };
        _db.EmploymentContracts.Add(contract);
        await _db.SaveChangesAsync();

        var loaded = await _db.EmploymentContracts.AsNoTracking()
            .Include(c => c.Employee)
            .FirstAsync(c => c.Id == contract.Id);
        return ToDto(loaded);
    }

    public async Task<EmploymentContractDto?> UpdateAsync(Guid id, UpdateContractRequest r)
    {
        var contract = await _db.EmploymentContracts.FindAsync(id);
        if (contract is null) return null;

        contract.ContractType = r.ContractType;
        contract.StartDate = r.StartDate;
        contract.EndDate = r.EndDate;
        contract.GrossSalary = r.GrossSalary;
        contract.Currency = r.Currency;
        contract.IsActive = r.IsActive;

        await _db.SaveChangesAsync();

        var loaded = await _db.EmploymentContracts.AsNoTracking()
            .Include(c => c.Employee)
            .FirstAsync(c => c.Id == id);
        return ToDto(loaded);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var contract = await _db.EmploymentContracts.FindAsync(id);
        if (contract is null) return false;
        _db.EmploymentContracts.Remove(contract);
        await _db.SaveChangesAsync();
        return true;
    }

    private static EmploymentContractDto ToDto(EmploymentContract c) => new(
        c.Id, c.EmployeeId,
        c.Employee.FirstName + " " + c.Employee.LastName,
        c.ContractType, c.StartDate, c.EndDate, c.GrossSalary, c.Currency, c.IsActive);
}
