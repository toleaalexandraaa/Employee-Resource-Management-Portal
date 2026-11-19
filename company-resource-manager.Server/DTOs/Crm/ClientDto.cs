using company_resource_manager.Server.Models.Crm;

namespace company_resource_manager.Server.DTOs.Crm;

public record ClientListItemDto(
    Guid Id,
    string CompanyName,
    string? Industry,
    string? Country,
    ClientStatus Status,
    string AccountManagerName,
    int ContactCount);

public record ClientDto(
    Guid Id,
    string CompanyName,
    string? Industry,
    string? Website,
    string? Address,
    string? Country,
    Guid AccountManagerId,
    string AccountManagerName,
    ClientStatus Status,
    IReadOnlyList<ClientContactDto> Contacts);

public record CreateClientRequest(
    string CompanyName,
    string? Industry,
    string? Website,
    string? Address,
    string? Country,
    Guid AccountManagerId,
    ClientStatus Status = ClientStatus.Active);

public record UpdateClientRequest(
    string CompanyName,
    string? Industry,
    string? Website,
    string? Address,
    string? Country,
    Guid AccountManagerId,
    ClientStatus Status);
