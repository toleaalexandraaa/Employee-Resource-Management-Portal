using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Estimation;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Estimation.ML;
using company_resource_manager.Server.Services.Interfaces;
using company_resource_manager.Server.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace company_resource_manager.Server.Services.Estimation;

public class EstimationService : IEstimationService
{
    private const int MinTrainingSamples = 10;
    private static readonly string[] FeatureColumns =
    [
        nameof(ProjectEstimationInput.TeamSize),
        nameof(ProjectEstimationInput.EstimatedHoursTotal),
        nameof(ProjectEstimationInput.TaskCount),
        nameof(ProjectEstimationInput.HasFixedDeadline),
        nameof(ProjectEstimationInput.AverageSeniorityYears),
        nameof(ProjectEstimationInput.IsFirstProjectWithClient),
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _modelDirectory;
    private readonly ILogger<EstimationService> _logger;

    private readonly object _lock = new();
    private ITransformer? _costModel;
    private ITransformer? _durationModel;
    private DataViewSchema? _inputSchema;
    private float _costR2;
    private float _durationR2;
    private string _modelVersion = string.Empty;

    public EstimationService(
        IServiceScopeFactory scopeFactory,
        IOptions<EstimationSettings> settings,
        ILogger<EstimationService> logger)
    {
        _scopeFactory = scopeFactory;
        _modelDirectory = settings.Value.ModelDirectory;
        _logger = logger;
    }

    public EstimateProjectResponse GetModelStatus()
    {
        lock (_lock)
        {
            bool ready = _costModel is not null && _durationModel is not null;
            return new EstimateProjectResponse(
                IsModelReady: ready,
                EstimatedCost: null,
                EstimatedDurationDays: null,
                CostModelR2: _costR2,
                DurationModelR2: _durationR2,
                ModelVersion: _modelVersion,
                Message: ready ? null : "Model not trained yet. Trigger /api/estimation/retrain or wait for startup training.");
        }
    }

    public Task<EstimateProjectResponse> EstimateAsync(EstimateProjectRequest request)
    {
        lock (_lock)
        {
            if (_costModel is null || _durationModel is null || _inputSchema is null)
            {
                return Task.FromResult(new EstimateProjectResponse(
                    IsModelReady: false,
                    EstimatedCost: null,
                    EstimatedDurationDays: null,
                    CostModelR2: _costR2,
                    DurationModelR2: _durationR2,
                    ModelVersion: _modelVersion,
                    Message: "Model not ready. Insufficient training data or model not yet trained."));
            }

            var mlContext = new MLContext();
            var input = ToInput(request);

            var costEngine = mlContext.Model.CreatePredictionEngine<ProjectEstimationInput, ProjectEstimationOutput>(
                _costModel, _inputSchema);
            var durationEngine = mlContext.Model.CreatePredictionEngine<ProjectEstimationInput, ProjectEstimationOutput>(
                _durationModel, _inputSchema);

            var cost = costEngine.Predict(input);
            var duration = durationEngine.Predict(input);

            return Task.FromResult(new EstimateProjectResponse(
                IsModelReady: true,
                EstimatedCost: (decimal)Math.Max(0, cost.Score),
                EstimatedDurationDays: (int)Math.Max(1, Math.Round(duration.Score)),
                CostModelR2: _costR2,
                DurationModelR2: _durationR2,
                ModelVersion: _modelVersion,
                Message: null));
        }
    }

    public async Task<EstimateProjectResponse> RetrainAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting estimation model training...");

        List<ProjectEstimationInput> costSamples;
        List<ProjectEstimationInput> durationSamples;

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (costSamples, durationSamples) = await BuildTrainingSamplesAsync(db, ct);
        }

        if (costSamples.Count < MinTrainingSamples)
        {
            _logger.LogWarning("Not enough completed projects to train ({Count}/{Min}).", costSamples.Count, MinTrainingSamples);
            return new EstimateProjectResponse(
                IsModelReady: false,
                EstimatedCost: null,
                EstimatedDurationDays: null,
                CostModelR2: 0,
                DurationModelR2: 0,
                ModelVersion: string.Empty,
                Message: $"Insufficient training data ({costSamples.Count}/{MinTrainingSamples} completed projects required).");
        }

        var mlContext = new MLContext(seed: 42);
        Directory.CreateDirectory(_modelDirectory);

        var (costModel, costR2, costSchema) = TrainModel(mlContext, costSamples, "cost");
        var (durationModel, durationR2, _) = TrainModel(mlContext, durationSamples, "duration");

        var version = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        lock (_lock)
        {
            _costModel = costModel;
            _durationModel = durationModel;
            _inputSchema = costSchema;
            _costR2 = costR2;
            _durationR2 = durationR2;
            _modelVersion = version;
        }

        _logger.LogInformation("Models trained. Cost R²={CostR2:F3}, Duration R²={DurR2:F3}, Version={Version}",
            costR2, durationR2, version);

        return new EstimateProjectResponse(
            IsModelReady: true,
            EstimatedCost: null,
            EstimatedDurationDays: null,
            CostModelR2: costR2,
            DurationModelR2: durationR2,
            ModelVersion: version,
            Message: $"Training complete. {costSamples.Count} samples used.");
    }

    // ── private helpers ───────────────────────────────────────────────────────

    private (ITransformer model, float r2, DataViewSchema schema) TrainModel(
        MLContext mlContext,
        List<ProjectEstimationInput> samples,
        string modelName)
    {
        var dataView = mlContext.Data.LoadFromEnumerable(samples);
        var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        var pipeline = mlContext.Transforms
            .Concatenate("Features", FeatureColumns)
            .Append(mlContext.Regression.Trainers.Sdca(
                labelColumnName: "Label",
                maximumNumberOfIterations: 100));

        var model = pipeline.Fit(split.TrainSet);
        var predictions = model.Transform(split.TestSet);
        var metrics = mlContext.Regression.Evaluate(predictions, labelColumnName: "Label");

        var schema = dataView.Schema;

        var costModelPath = Path.Combine(_modelDirectory, $"{modelName}-model.zip");
        mlContext.Model.Save(model, schema, costModelPath);
        _logger.LogInformation("{Model} model R²={R2:F3} saved to {Path}", modelName, metrics.RSquared, costModelPath);

        return (model, (float)metrics.RSquared, schema);
    }

    private static async Task<(List<ProjectEstimationInput> cost, List<ProjectEstimationInput> duration)>
        BuildTrainingSamplesAsync(AppDbContext db, CancellationToken ct)
    {
        var completedProjects = await db.Projects
            .AsNoTracking()
            .Where(p => p.Status == ProjectStatus.Completed && p.ActualBudget > 0 && p.EndDate != null)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .ToListAsync(ct);

        var employeeIds = completedProjects
            .SelectMany(p => p.Members.Select(m => m.EmployeeId))
            .Distinct()
            .ToList();

        var seniorityMap = await db.Employees
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.SeniorityYears, ct);

        var clientProjectCounts = completedProjects
            .Where(p => p.ClientId.HasValue)
            .GroupBy(p => p.ClientId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var costSamples = new List<ProjectEstimationInput>();
        var durationSamples = new List<ProjectEstimationInput>();

        foreach (var p in completedProjects)
        {
            var teamSize = p.Members.Count;
            var estimatedHours = (float)p.Tasks.Sum(t => t.EstimatedHours);
            var taskCount = p.Tasks.Count;
            var hasFixedDeadline = p.EndDate.HasValue ? 1f : 0f;

            var avgSeniority = p.Members.Count > 0
                ? p.Members.Average(m => seniorityMap.GetValueOrDefault(m.EmployeeId))
                : 0f;

            var isFirstClient = p.ClientId.HasValue && clientProjectCounts.GetValueOrDefault(p.ClientId.Value) <= 1
                ? 1f : 0f;

            var actualDuration = (float)(p.EndDate!.Value.DayNumber - p.StartDate.DayNumber);

            costSamples.Add(new ProjectEstimationInput
            {
                TeamSize = teamSize,
                EstimatedHoursTotal = estimatedHours,
                TaskCount = taskCount,
                HasFixedDeadline = hasFixedDeadline,
                AverageSeniorityYears = avgSeniority,
                IsFirstProjectWithClient = isFirstClient,
                Label = (float)p.ActualBudget,
            });

            durationSamples.Add(new ProjectEstimationInput
            {
                TeamSize = teamSize,
                EstimatedHoursTotal = estimatedHours,
                TaskCount = taskCount,
                HasFixedDeadline = hasFixedDeadline,
                AverageSeniorityYears = avgSeniority,
                IsFirstProjectWithClient = isFirstClient,
                Label = actualDuration,
            });
        }

        return (costSamples, durationSamples);
    }

    private static ProjectEstimationInput ToInput(EstimateProjectRequest r) => new()
    {
        TeamSize = r.TeamSize,
        EstimatedHoursTotal = r.EstimatedHoursTotal,
        TaskCount = r.TaskCount,
        HasFixedDeadline = r.HasFixedDeadline ? 1f : 0f,
        AverageSeniorityYears = r.AverageSeniorityYears,
        IsFirstProjectWithClient = r.IsFirstProjectWithClient ? 1f : 0f,
        Label = 0f,
    };
}
