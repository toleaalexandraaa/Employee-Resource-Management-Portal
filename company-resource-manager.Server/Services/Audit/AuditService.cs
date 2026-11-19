using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Audit;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Services.Audit;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetLogsAsync(
        string? entityName,
        string? entityId,
        int page = 1,
        int pageSize = 50)
    {
        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(a => a.EntityId == entityId);

        return await query
            .OrderByDescending(a => a.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.EntityName,
                a.EntityId,
                a.Action,
                a.OldValues,
                a.NewValues,
                a.ChangedBy,
                a.ChangedAt))
            .ToListAsync();
    }
}
