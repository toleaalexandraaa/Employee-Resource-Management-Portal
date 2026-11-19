using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Crm;

public class LeadService : ILeadService
{
    private readonly AppDbContext _db;
    private readonly IClientService _clientService;
    private readonly UserManager<Models.Identity.ApplicationUser> _userManager;

    public LeadService(
        AppDbContext db,
        IClientService clientService,
        UserManager<Models.Identity.ApplicationUser> userManager)
    {
        _db = db;
        _clientService = clientService;
        _userManager = userManager;
    }

    public async Task<PagedResult<LeadDto>> GetAllAsync(int page, int pageSize)
    {
        var query = _db.Leads.AsNoTracking();
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var userIds = items.Select(l => l.AssignedToUserId).Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        return new PagedResult<LeadDto>(
            items.Select(l => ToDto(l, users.GetValueOrDefault(l.AssignedToUserId, "Unknown"))).ToList(),
            total, page, pageSize);
    }

    public async Task<LeadDto?> GetByIdAsync(Guid id)
    {
        var lead = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        if (lead is null) return null;
        var user = await _userManager.FindByIdAsync(lead.AssignedToUserId.ToString());
        return ToDto(lead, user?.FullName ?? "Unknown");
    }

    public async Task<LeadDto> CreateAsync(CreateLeadRequest r)
    {
        var lead = new Lead
        {
            CompanyName = r.CompanyName,
            ContactName = r.ContactName,
            ContactEmail = r.ContactEmail,
            ContactPhone = r.ContactPhone,
            Source = r.Source,
            Stage = LeadStage.New,
            EstimatedValue = r.EstimatedValue,
            AssignedToUserId = r.AssignedToUserId,
            Notes = r.Notes,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(lead.Id) ?? throw new InvalidOperationException("Lead not found after creation.");
    }

    public async Task<LeadDto?> UpdateAsync(Guid id, UpdateLeadRequest r)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead is null) return null;

        lead.CompanyName = r.CompanyName;
        lead.ContactName = r.ContactName;
        lead.ContactEmail = r.ContactEmail;
        lead.ContactPhone = r.ContactPhone;
        lead.Source = r.Source;
        lead.Stage = r.Stage;
        lead.EstimatedValue = r.EstimatedValue;
        lead.AssignedToUserId = r.AssignedToUserId;
        lead.Notes = r.Notes;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<LeadDto?> AdvanceStageAsync(Guid id)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead is null) return null;

        // Cycle: New → Contacted → Qualified → ProposalSent → Won (terminal)
        lead.Stage = lead.Stage switch
        {
            LeadStage.New => LeadStage.Contacted,
            LeadStage.Contacted => LeadStage.Qualified,
            LeadStage.Qualified => LeadStage.ProposalSent,
            LeadStage.ProposalSent => LeadStage.Won,
            _ => lead.Stage,
        };

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<ClientDto> ConvertToClientAsync(Guid id, ConvertLeadRequest r)
    {
        var lead = await _db.Leads.FindAsync(id)
            ?? throw new InvalidOperationException("Lead not found.");

        var client = await _clientService.CreateAsync(new CreateClientRequest(
            lead.CompanyName,
            Industry: null,
            Website: null,
            Address: null,
            Country: null,
            AccountManagerId: r.AccountManagerId,
            Status: r.Status));

        lead.ConvertedToClientId = client.Id;
        lead.Stage = LeadStage.Won;
        await _db.SaveChangesAsync();

        return client;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead is null) return false;
        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync();
        return true;
    }

    private static LeadDto ToDto(Lead l, string assignedUserName) => new(
        l.Id, l.CompanyName, l.ContactName, l.ContactEmail, l.ContactPhone,
        l.Source, l.Stage, l.EstimatedValue, l.AssignedToUserId, assignedUserName,
        l.Notes, l.ConvertedToClientId);
}
