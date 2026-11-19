namespace company_resource_manager.Server.DTOs.Audit;

public record AuditLogDto(
    Guid Id,
    string EntityName,
    string EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    Guid ChangedBy,
    DateTime ChangedAt
);
