using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Models.Shared;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Crm;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly UserManager<Models.Identity.ApplicationUser> _userManager;

    public InvoiceService(
        AppDbContext db,
        INotificationService notifications,
        UserManager<Models.Identity.ApplicationUser> userManager)
    {
        _db = db;
        _notifications = notifications;
        _userManager = userManager;
    }

    public async Task<PagedResult<InvoiceListItemDto>> GetAllAsync(int page, int pageSize)
    {
        var query = _db.Invoices.AsNoTracking().Include(i => i.Client);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.IssueDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InvoiceListItemDto(
                i.Id, i.InvoiceNumber, i.ClientId, i.Client.CompanyName,
                i.IssueDate, i.DueDate, i.TotalAmount, i.PaidAmount, i.Currency, i.Status))
            .ToListAsync();
        return new PagedResult<InvoiceListItemDto>(items, total, page, pageSize);
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var invoice = await _db.Invoices.AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null) return null;

        string? projectName = null;
        if (invoice.ProjectId.HasValue)
        {
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == invoice.ProjectId.Value);
            projectName = project?.Name;
        }
        return ToDto(invoice, projectName);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceRequest r)
    {
        var year = DateTime.UtcNow.Year;
        var seq = await _db.Invoices.CountAsync(i =>
            i.InvoiceNumber.StartsWith($"INV-{year}-")) + 1;

        var lineItems = r.LineItems.Select(li => new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            Description = li.Description,
            Quantity = li.Quantity,
            UnitPrice = li.UnitPrice,
            LineTotal = li.Quantity * li.UnitPrice,
        }).ToList();

        var subTotal = lineItems.Sum(li => li.LineTotal);
        var taxAmount = subTotal * r.TaxRate;

        var invoice = new Invoice
        {
            ClientId = r.ClientId,
            ContractId = r.ContractId,
            ProjectId = r.ProjectId,
            InvoiceNumber = $"INV-{year}-{seq:D3}",
            IssueDate = r.IssueDate,
            DueDate = r.DueDate,
            SubTotal = subTotal,
            TaxRate = r.TaxRate,
            TaxAmount = taxAmount,
            TotalAmount = subTotal + taxAmount,
            Currency = r.Currency,
            Status = InvoiceStatus.Draft,
            LineItems = lineItems,
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(invoice.Id) ?? throw new InvalidOperationException("Invoice not found after creation.");
    }

    public async Task<InvoiceDto> GenerateFromTimeEntriesAsync(GenerateInvoiceFromTimeEntriesRequest r)
    {
        var entries = await _db.TimeEntries.AsNoTracking()
            .Include(e => e.Task)
            .Where(e => e.ProjectId == r.ProjectId &&
                        e.Date >= r.DateFrom && e.Date <= r.DateTo)
            .ToListAsync();

        if (!entries.Any())
            throw new InvalidOperationException("No time entries found for the specified period.");

        var grouped = entries
            .GroupBy(e => e.Task?.Title ?? "Unknown Task")
            .Select(g => new CreateInvoiceLineItemRequest(
                g.Key,
                g.Sum(e => e.HoursLogged),
                r.HourlyRate))
            .ToList();

        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        return await CreateAsync(new CreateInvoiceRequest(
            r.ClientId, r.ContractId, r.ProjectId,
            issueDate, issueDate.AddDays(r.DueDays),
            0m, r.Currency, grouped));
    }

    public async Task<InvoiceDto?> SendAsync(Guid id)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return null;

        invoice.Status = InvoiceStatus.Sent;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<InvoiceDto?> RecordPaymentAsync(Guid id, RecordPaymentRequest r)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Client)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null) return null;

        invoice.PaidAmount += r.Amount;

        if (invoice.PaidAmount >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = r.PaidAt ?? DateTime.UtcNow;
        }
        else
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        await _db.SaveChangesAsync();

        // Notify account manager for the client
        var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == invoice.ClientId);
        if (client is not null)
        {
            await _notifications.CreateAsync(
                client.AccountManagerId,
                NotificationType.InvoicePaymentRecorded,
                "Invoice Payment Recorded",
                $"Payment of {r.Amount:C} recorded for invoice {invoice.InvoiceNumber}. Status: {invoice.Status}.");
        }

        return ToDto(invoice, null);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var invoice = await _db.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null) return false;
        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync();
        return true;
    }

    private static InvoiceDto ToDto(Invoice i, string? projectName) => new(
        i.Id, i.InvoiceNumber, i.ClientId, i.Client.CompanyName,
        i.ContractId, i.ProjectId, projectName,
        i.IssueDate, i.DueDate, i.SubTotal, i.TaxRate, i.TaxAmount, i.TotalAmount,
        i.Currency, i.Status, i.PaidAmount, i.PaidAt,
        i.LineItems.Select(li => new InvoiceLineItemDto(
            li.Id, li.Description, li.Quantity, li.UnitPrice, li.LineTotal)).ToList());
}
