namespace company_resource_manager.Server.DTOs.Crm;

public record ClientContactDto(
    Guid Id,
    Guid ClientId,
    string FullName,
    string? JobTitle,
    string Email,
    string? Phone,
    bool IsPrimary);

public record CreateClientContactRequest(
    string FullName,
    string? JobTitle,
    string Email,
    string? Phone,
    bool IsPrimary = false);

public record UpdateClientContactRequest(
    string FullName,
    string? JobTitle,
    string Email,
    string? Phone,
    bool IsPrimary);
