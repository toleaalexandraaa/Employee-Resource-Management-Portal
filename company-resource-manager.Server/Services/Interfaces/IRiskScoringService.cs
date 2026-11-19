using company_resource_manager.Server.DTOs.Analytics;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IRiskScoringService
{
    Task<ProjectRisksDto> GetProjectRisksAsync(CancellationToken ct = default);
    Task<ProjectRiskScore?> GetProjectRiskAsync(Guid projectId, CancellationToken ct = default);
}
