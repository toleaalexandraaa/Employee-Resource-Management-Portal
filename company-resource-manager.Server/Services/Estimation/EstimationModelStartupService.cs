using company_resource_manager.Server.Services.Interfaces;

namespace company_resource_manager.Server.Services.Estimation;

/// <summary>
/// Triggers model training/loading once at application startup.
/// </summary>
public class EstimationModelStartupService : IHostedService
{
    private readonly IEstimationService _estimationService;
    private readonly ILogger<EstimationModelStartupService> _logger;

    public EstimationModelStartupService(
        IEstimationService estimationService,
        ILogger<EstimationModelStartupService> logger)
    {
        _estimationService = estimationService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Estimation startup: attempting model training/loading...");
        var result = await _estimationService.RetrainAsync(cancellationToken);

        if (result.IsModelReady)
            _logger.LogInformation("Estimation model ready. Version={Version}", result.ModelVersion);
        else
            _logger.LogInformation("Estimation model not ready: {Message}", result.Message);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
