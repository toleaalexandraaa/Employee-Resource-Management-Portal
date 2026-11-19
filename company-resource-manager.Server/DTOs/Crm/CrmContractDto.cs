using company_resource_manager.Server.Models.Crm;

namespace company_resource_manager.Server.DTOs.Crm;

public record CrmContractDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid? ProjectId,
    string? ProjectName,
    string ContractNumber,
    string Title,
    decimal Value,
    string Currency,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? SignedDate,
    ContractStatus Status,
    string? DocumentPath);

public record CreateCrmContractRequest(
    Guid ClientId,
    Guid? ProjectId,
    string Title,
    decimal Value,
    string Currency,
    DateOnly StartDate,
    DateOnly? EndDate);

public record UpdateCrmContractRequest(
    Guid? ProjectId,
    string Title,
    decimal Value,
    string Currency,
    DateOnly StartDate,
    DateOnly? EndDate,
    ContractStatus Status);
