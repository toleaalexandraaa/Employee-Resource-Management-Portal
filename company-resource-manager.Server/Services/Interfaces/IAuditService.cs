using company_resource_manager.Server.DTOs.Audit;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IAuditService
{
    Task<IReadOnlyList<AuditLogDto>> GetLogsAsync(
        string? entityName,
        string? entityId,
        int page = 1,
        int pageSize = 50);
}
