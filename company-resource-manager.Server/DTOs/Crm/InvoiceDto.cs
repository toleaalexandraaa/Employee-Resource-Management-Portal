using company_resource_manager.Server.Models.Crm;

namespace company_resource_manager.Server.DTOs.Crm;

public record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    Guid ClientId,
    string ClientName,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal PaidAmount,
    string Currency,
    InvoiceStatus Status);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid ClientId,
    string ClientName,
    Guid? ContractId,
    Guid? ProjectId,
    string? ProjectName,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal SubTotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    InvoiceStatus Status,
    decimal PaidAmount,
    DateTime? PaidAt,
    IReadOnlyList<InvoiceLineItemDto> LineItems);

public record InvoiceLineItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record CreateInvoiceRequest(
    Guid ClientId,
    Guid? ContractId,
    Guid? ProjectId,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal TaxRate,
    string Currency,
    IReadOnlyList<CreateInvoiceLineItemRequest> LineItems);

public record CreateInvoiceLineItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record UpdateInvoiceRequest(
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal TaxRate,
    string Currency,
    IReadOnlyList<CreateInvoiceLineItemRequest> LineItems);

public record GenerateInvoiceFromTimeEntriesRequest(
    Guid ProjectId,
    Guid ClientId,
    Guid? ContractId,
    DateOnly DateFrom,
    DateOnly DateTo,
    decimal HourlyRate,
    string Currency,
    int DueDays);

public record RecordPaymentRequest(decimal Amount, DateTime? PaidAt);
