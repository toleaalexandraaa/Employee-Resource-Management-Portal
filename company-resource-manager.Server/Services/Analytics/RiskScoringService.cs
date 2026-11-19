using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Analytics;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Analytics.ML;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using TaskStatus = company_resource_manager.Server.Models.Proj.TaskStatus;

namespace company_resource_manager.Server.Services.Analytics;

public class RiskScoringService : IRiskScoringService
{
    private const int MinTrainingSamples = 20;
    private const int MinPositiveSamples = 2;  // need at least 2 at-risk projects to train

    private static readonly string[] FeatureColumns =
    [
        nameof(ProjectRiskInput.BudgetBurnRate),
        nameof(ProjectRiskInput.TimeBurnRate),
        nameof(ProjectRiskInput.TimeElapsedRatio),
        nameof(ProjectRiskInput.OverdueTaskCount),
        nameof(ProjectRiskInput.TeamUtilizationRate),
        nameof(ProjectRiskInput.WipViolationsCount),
    ];

    private readonly AppDbContext _db;
    private readonly ILogger<RiskScoringService> _logger;

    public RiskScoringService(AppDbContext db, ILogger<RiskScoringService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ProjectRisksDto> GetProjectRisksAsync(CancellationToken ct = default)
    {
        var (trainingSamples, trainingMeta) = await BuildTrainingSamplesAsync(ct);

        if (trainingSamples.Count < MinTrainingSamples)
        {
            return new ProjectRisksDto(
                [],
                false,
                $"Insufficient training data: {trainingSamples.Count}/{MinTrainingSamples} completed projects required. Showing rule-based scores instead.");
        }

        int positiveCount = trainingSamples.Count(s => s.Label);
        if (positiveCount < MinPositiveSamples)
        {
            return new ProjectRisksDto(
                [],
                false,
                $"Insufficient at-risk projects in history ({positiveCount}). Cannot train binary classifier.");
        }

        var model = TrainModel(trainingSamples);

        var activeInputs = await BuildActiveProjectInputsAsync(ct);
        if (activeInputs.Count == 0)
        {
            return new ProjectRisksDto([], true, "No active projects to score.");
        }

        var scores = ScoreProjects(model, activeInputs);
        return new ProjectRisksDto(scores, true, $"Scored {scores.Count} active projects (trained on {trainingSamples.Count} completed projects).");
    }

    public async Task<ProjectRiskScore?> GetProjectRiskAsync(Guid projectId, CancellationToken ct = default)
    {
        var all = await GetProjectRisksAsync(ct);
        return all.Projects.FirstOrDefault(p => p.ProjectId == projectId);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private ITransformer TrainModel(List<ProjectRiskInput> samples)
    {
        var mlContext = new MLContext(seed: 42);
        var dataView = mlContext.Data.LoadFromEnumerable(samples);

        var pipeline = mlContext.Transforms
            .Concatenate("Features", FeatureColumns)
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(ProjectRiskInput.Label),
                featureColumnName: "Features"));

        return pipeline.Fit(dataView);
    }

    private List<ProjectRiskScore> ScoreProjects(ITransformer model, List<(Guid Id, string Name, ProjectRiskInput Input)> inputs)
    {
        var mlContext = new MLContext(seed: 42);
        var dataView = mlContext.Data.LoadFromEnumerable(inputs.Select(i => i.Input));
        var predictions = mlContext.Data
            .CreateEnumerable<RiskPrediction>(model.Transform(dataView), reuseRowObject: false)
            .ToList();

        return inputs.Select((item, idx) =>
        {
            var prob = predictions.Count > idx ? predictions[idx].Probability : 0f;
            var score = (int)Math.Round(prob * 100);
            var level = score < 40 ? "OnTrack" : score < 70 ? "Watch" : "AtRisk";

            return new ProjectRiskScore(
                item.Id,
                item.Name,
                score,
                level,
                item.Input.BudgetBurnRate,
                item.Input.TimeBurnRate,
                item.Input.TimeElapsedRatio,
                (int)item.Input.OverdueTaskCount);
        }).ToList();
    }

    private async Task<(List<ProjectRiskInput> Samples, List<object> Meta)> BuildTrainingSamplesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var completed = await _db.Projects
            .AsNoTracking()
            .Where(p => p.Status == ProjectStatus.Completed)
            .Include(p => p.Tasks)
            .Include(p => p.Members)
            .ToListAsync(ct);

        var timeEntries = await _db.TimeEntries
            .AsNoTracking()
            .ToListAsync(ct);

        var hoursByProject = timeEntries
            .GroupBy(te => te.ProjectId)
            .ToDictionary(g => g.Key, g => (float)g.Sum(te => te.HoursLogged));

        var samples = new List<ProjectRiskInput>();

        foreach (var p in completed)
        {
            if (p.EstimatedBudget <= 0) continue;

            var estimatedHours = (float)p.Tasks.Sum(t => t.EstimatedHours);
            var actualHours = hoursByProject.GetValueOrDefault(p.Id, 0f);
            var actualDuration = p.EndDate.HasValue
                ? (float)(p.EndDate.Value.DayNumber - p.StartDate.DayNumber)
                : 0f;
            var estimatedDuration = (float)p.EstimatedDurationDays;

            var budgetBurn = p.ActualBudget > 0 ? (float)(p.ActualBudget / p.EstimatedBudget) : 1f;
            var timeBurn = estimatedHours > 0 ? actualHours / estimatedHours : 1f;
            var timeElapsed = estimatedDuration > 0 ? actualDuration / estimatedDuration : 1f;

            // Label: at risk = ran late OR over budget
            bool wasLate = p.EndDate.HasValue && p.EstimatedDurationDays > 0
                && actualDuration > estimatedDuration;
            bool wasOverBudget = p.ActualBudget > p.EstimatedBudget;
            bool label = wasLate || wasOverBudget;

            samples.Add(new ProjectRiskInput
            {
                BudgetBurnRate = Math.Min(budgetBurn, 3f),
                TimeBurnRate = Math.Min(timeBurn, 3f),
                TimeElapsedRatio = Math.Min(timeElapsed, 3f),
                OverdueTaskCount = 0f,  // no overdue tasks for completed projects
                TeamUtilizationRate = 1f,
                WipViolationsCount = 0f,
                Label = label,
            });
        }

        return (samples, []);
    }

    private async Task<List<(Guid Id, string Name, ProjectRiskInput Input)>> BuildActiveProjectInputsAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var active = await _db.Projects
            .AsNoTracking()
            .Where(p => p.Status == ProjectStatus.Active)
            .Include(p => p.Tasks)
            .Include(p => p.Members)
            .ToListAsync(ct);

        var timeEntries = await _db.TimeEntries
            .AsNoTracking()
            .Where(te => te.Date >= today.AddDays(-7))
            .ToListAsync(ct);

        var hoursByProject = timeEntries
            .GroupBy(te => te.ProjectId)
            .ToDictionary(g => g.Key, g => (float)g.Sum(te => te.HoursLogged));

        var allTimeEntries = await _db.TimeEntries
            .AsNoTracking()
            .ToListAsync(ct);

        var totalHoursByProject = allTimeEntries
            .GroupBy(te => te.ProjectId)
            .ToDictionary(g => g.Key, g => (float)g.Sum(te => te.HoursLogged));

        var kanbanBoards = await _db.KanbanBoards
            .AsNoTracking()
            .Include(b => b.Columns)
                .ThenInclude(col => col.Tasks)
            .Where(b => active.Select(p => p.Id).Contains(b.ProjectId))
            .ToListAsync(ct);

        var result = new List<(Guid, string, ProjectRiskInput)>();

        foreach (var p in active)
        {
            var estimatedHours = (float)p.Tasks.Sum(t => t.EstimatedHours);
            var totalLogged = totalHoursByProject.GetValueOrDefault(p.Id, 0f);
            var daysElapsed = (float)(today.DayNumber - p.StartDate.DayNumber);
            var estimatedDuration = (float)p.EstimatedDurationDays;

            var budgetBurn = p.EstimatedBudget > 0
                ? (float)(p.ActualBudget / p.EstimatedBudget)
                : 0f;
            var timeBurn = estimatedHours > 0 ? totalLogged / estimatedHours : 0f;
            var timeElapsed = estimatedDuration > 0 ? daysElapsed / estimatedDuration : 0f;

            var overdueCount = p.Tasks.Count(t =>
                t.Status != TaskStatus.Done &&
                t.DueDate.HasValue &&
                t.DueDate.Value < today);

            // Team utilization: actual hours this week vs allocated
            var weekHours = hoursByProject.GetValueOrDefault(p.Id, 0f);
            var allocatedPerWeek = (float)p.Members.Sum(m => m.AllocatedHoursPerWeek);
            var utilization = allocatedPerWeek > 0 ? weekHours / allocatedPerWeek : 0f;

            // WIP violations
            var board = kanbanBoards.FirstOrDefault(b => b.ProjectId == p.Id);
            int wipViolations = 0;
            if (board != null)
            {
                wipViolations = board.Columns
                    .Count(col => col.WipLimit.HasValue && col.Tasks.Count > col.WipLimit.Value);
            }

            result.Add((p.Id, p.Name, new ProjectRiskInput
            {
                BudgetBurnRate = Math.Min(budgetBurn, 3f),
                TimeBurnRate = Math.Min(timeBurn, 3f),
                TimeElapsedRatio = Math.Min(timeElapsed, 3f),
                OverdueTaskCount = overdueCount,
                TeamUtilizationRate = Math.Min(utilization, 3f),
                WipViolationsCount = wipViolations,
                Label = false,  // dummy for active projects
            }));
        }

        return result;
    }
}
