using company_resource_manager.Server.Models.Crm;

namespace company_resource_manager.Server.DTOs.Crm;

public record LeadDto(
    Guid Id,
    string CompanyName,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    LeadSource Source,
    LeadStage Stage,
    decimal? EstimatedValue,
    Guid AssignedToUserId,
    string AssignedToUserName,
    string? Notes,
    Guid? ConvertedToClientId);

public record CreateLeadRequest(
    string CompanyName,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    LeadSource Source,
    decimal? EstimatedValue,
    Guid AssignedToUserId,
    string? Notes);

public record UpdateLeadRequest(
    string CompanyName,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    LeadSource Source,
    LeadStage Stage,
    decimal? EstimatedValue,
    Guid AssignedToUserId,
    string? Notes);

public record ConvertLeadRequest(
    Guid AccountManagerId,
    ClientStatus Status = ClientStatus.Active);
