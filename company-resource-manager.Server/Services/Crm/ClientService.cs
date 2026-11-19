using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Crm;

public class ClientService : IClientService
{
    private readonly AppDbContext _db;
    private readonly UserManager<Models.Identity.ApplicationUser> _userManager;

    public ClientService(AppDbContext db, UserManager<Models.Identity.ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<PagedResult<ClientListItemDto>> GetAllAsync(int page, int pageSize)
    {
        var query = _db.Clients.AsNoTracking().Include(c => c.Contacts);
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.CompanyName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var managerIds = items.Select(c => c.AccountManagerId).Distinct().ToList();
        var managers = await _userManager.Users
            .Where(u => managerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        return new PagedResult<ClientListItemDto>(
            items.Select(c => new ClientListItemDto(
                c.Id, c.CompanyName, c.Industry, c.Country, c.Status,
                managers.GetValueOrDefault(c.AccountManagerId, "Unknown"),
                c.Contacts.Count)).ToList(),
            total, page, pageSize);
    }

    public async Task<ClientDto?> GetByIdAsync(Guid id)
    {
        var client = await _db.Clients.AsNoTracking()
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null) return null;

        var manager = await _userManager.FindByIdAsync(client.AccountManagerId.ToString());
        return ToDto(client, manager?.FullName ?? "Unknown");
    }

    public async Task<ClientDto> CreateAsync(CreateClientRequest r)
    {
        var client = new Client
        {
            CompanyName = r.CompanyName,
            Industry = r.Industry,
            Website = r.Website,
            Address = r.Address,
            Country = r.Country,
            AccountManagerId = r.AccountManagerId,
            Status = r.Status,
        };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(client.Id) ?? throw new InvalidOperationException("Client not found after creation.");
    }

    public async Task<ClientDto?> UpdateAsync(Guid id, UpdateClientRequest r)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client is null) return null;

        client.CompanyName = r.CompanyName;
        client.Industry = r.Industry;
        client.Website = r.Website;
        client.Address = r.Address;
        client.Country = r.Country;
        client.AccountManagerId = r.AccountManagerId;
        client.Status = r.Status;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client is null) return false;
        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<ClientContactDto>> GetContactsAsync(Guid clientId)
    {
        return await _db.ClientContacts.AsNoTracking()
            .Where(c => c.ClientId == clientId)
            .Select(c => ToContactDto(c))
            .ToListAsync();
    }

    public async Task<ClientContactDto> AddContactAsync(Guid clientId, CreateClientContactRequest r)
    {
        var contact = new ClientContact
        {
            ClientId = clientId,
            FullName = r.FullName,
            JobTitle = r.JobTitle,
            Email = r.Email,
            Phone = r.Phone,
            IsPrimary = r.IsPrimary,
        };
        _db.ClientContacts.Add(contact);
        await _db.SaveChangesAsync();
        return ToContactDto(contact);
    }

    public async Task<ClientContactDto?> UpdateContactAsync(Guid clientId, Guid contactId, UpdateClientContactRequest r)
    {
        var contact = await _db.ClientContacts.FirstOrDefaultAsync(c => c.Id == contactId && c.ClientId == clientId);
        if (contact is null) return null;

        contact.FullName = r.FullName;
        contact.JobTitle = r.JobTitle;
        contact.Email = r.Email;
        contact.Phone = r.Phone;
        contact.IsPrimary = r.IsPrimary;

        await _db.SaveChangesAsync();
        return ToContactDto(contact);
    }

    public async Task<bool> RemoveContactAsync(Guid clientId, Guid contactId)
    {
        var contact = await _db.ClientContacts.FirstOrDefaultAsync(c => c.Id == contactId && c.ClientId == clientId);
        if (contact is null) return false;
        _db.ClientContacts.Remove(contact);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ClientHistoryDto?> GetHistoryAsync(Guid clientId)
    {
        var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId);
        if (client is null) return null;

        var projects = await _db.Projects.AsNoTracking()
            .Where(p => p.ClientId == clientId)
            .Select(p => new ClientProjectSummary(p.Id, p.Name, p.Status, p.StartDate, p.EndDate))
            .ToListAsync();

        var contracts = await _db.CrmContracts.AsNoTracking()
            .Where(c => c.ClientId == clientId)
            .Select(c => new ClientContractSummary(c.Id, c.ContractNumber, c.Title, c.Value, c.Status))
            .ToListAsync();

        var invoices = await _db.Invoices.AsNoTracking()
            .Where(i => i.ClientId == clientId)
            .Select(i => new ClientInvoiceSummary(i.Id, i.InvoiceNumber, i.TotalAmount, i.Status, i.DueDate))
            .ToListAsync();

        return new ClientHistoryDto(clientId, client.CompanyName, projects, contracts, invoices);
    }

    private static ClientDto ToDto(Client c, string managerName) => new(
        c.Id, c.CompanyName, c.Industry, c.Website, c.Address, c.Country,
        c.AccountManagerId, managerName, c.Status,
        c.Contacts.Select(ToContactDto).ToList());

    private static ClientContactDto ToContactDto(ClientContact c) => new(
        c.Id, c.ClientId, c.FullName, c.JobTitle, c.Email, c.Phone, c.IsPrimary);
}
