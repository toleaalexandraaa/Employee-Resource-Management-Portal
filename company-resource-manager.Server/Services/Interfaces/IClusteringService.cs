using company_resource_manager.Server.DTOs.Analytics;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IClusteringService
{
    Task<EmployeeClustersDto> GetEmployeeClustersAsync(CancellationToken ct = default);
    Task<ClientSegmentsDto> GetClientSegmentsAsync(CancellationToken ct = default);
}
