using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Models.Proj;

namespace company_resource_manager.Server.DTOs.Crm;

public record ClientHistoryDto(
    Guid ClientId,
    string ClientName,
    IReadOnlyList<ClientProjectSummary> Projects,
    IReadOnlyList<ClientContractSummary> Contracts,
    IReadOnlyList<ClientInvoiceSummary> Invoices);

public record ClientProjectSummary(
    Guid Id,
    string Name,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate);

public record ClientContractSummary(
    Guid Id,
    string ContractNumber,
    string Title,
    decimal Value,
    ContractStatus Status);

public record ClientInvoiceSummary(
    Guid Id,
    string InvoiceNumber,
    decimal TotalAmount,
    InvoiceStatus Status,
    DateOnly DueDate);
