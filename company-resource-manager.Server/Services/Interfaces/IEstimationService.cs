using company_resource_manager.Server.DTOs.Estimation;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IEstimationService
{
    Task<EstimateProjectResponse> EstimateAsync(EstimateProjectRequest request);
    Task<EstimateProjectResponse> RetrainAsync(CancellationToken ct = default);
    EstimateProjectResponse GetModelStatus();
}
