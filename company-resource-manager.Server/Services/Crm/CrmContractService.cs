using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Crm;

public class CrmContractService : ICrmContractService
{
    private readonly AppDbContext _db;

    public CrmContractService(AppDbContext db) => _db = db;

    public async Task<PagedResult<CrmContractDto>> GetAllAsync(int page, int pageSize)
    {
        var query = _db.CrmContracts.AsNoTracking().Include(c => c.Client);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.StartDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var projectIds = items.Where(c => c.ProjectId.HasValue).Select(c => c.ProjectId!.Value).Distinct().ToList();
        var projects = projectIds.Any()
            ? await _db.Projects.AsNoTracking()
                .Where(p => projectIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name)
            : [];

        return new PagedResult<CrmContractDto>(
            items.Select(c => ToDto(c, projects.GetValueOrDefault(c.ProjectId ?? Guid.Empty))).ToList(),
            total, page, pageSize);
    }

    public async Task<CrmContractDto?> GetByIdAsync(Guid id)
    {
        var contract = await _db.CrmContracts.AsNoTracking()
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return null;

        string? projectName = null;
        if (contract.ProjectId.HasValue)
        {
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == contract.ProjectId.Value);
            projectName = project?.Name;
        }
        return ToDto(contract, projectName);
    }

    public async Task<CrmContractDto> CreateAsync(CreateCrmContractRequest r)
    {
        var year = DateTime.UtcNow.Year;
        var seq = await _db.CrmContracts.CountAsync(c =>
            c.ContractNumber.StartsWith($"CTR-{year}-")) + 1;

        var contract = new CrmContract
        {
            ClientId = r.ClientId,
            ProjectId = r.ProjectId,
            ContractNumber = $"CTR-{year}-{seq:D3}",
            Title = r.Title,
            Value = r.Value,
            Currency = r.Currency,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            Status = ContractStatus.Draft,
        };
        _db.CrmContracts.Add(contract);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(contract.Id) ?? throw new InvalidOperationException("Contract not found after creation.");
    }

    public async Task<CrmContractDto?> UpdateAsync(Guid id, UpdateCrmContractRequest r)
    {
        var contract = await _db.CrmContracts.FindAsync(id);
        if (contract is null) return null;

        contract.ProjectId = r.ProjectId;
        contract.Title = r.Title;
        contract.Value = r.Value;
        contract.Currency = r.Currency;
        contract.StartDate = r.StartDate;
        contract.EndDate = r.EndDate;
        contract.Status = r.Status;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<CrmContractDto?> SignAsync(Guid id)
    {
        var contract = await _db.CrmContracts.FindAsync(id);
        if (contract is null) return null;

        contract.Status = ContractStatus.Signed;
        contract.SignedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var contract = await _db.CrmContracts.FindAsync(id);
        if (contract is null) return false;
        _db.CrmContracts.Remove(contract);
        await _db.SaveChangesAsync();
        return true;
    }

    private static CrmContractDto ToDto(CrmContract c, string? projectName) => new(
        c.Id, c.ClientId, c.Client.CompanyName, c.ProjectId, projectName,
        c.ContractNumber, c.Title, c.Value, c.Currency,
        c.StartDate, c.EndDate, c.SignedDate, c.Status, c.DocumentPath);
}
