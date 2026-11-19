using Microsoft.ML.Data;

namespace company_resource_manager.Server.Services.Estimation.ML;

public class ProjectEstimationInput
{
    [LoadColumn(0)] public float TeamSize { get; set; }
    [LoadColumn(1)] public float EstimatedHoursTotal { get; set; }
    [LoadColumn(2)] public float TaskCount { get; set; }
    [LoadColumn(3)] public float HasFixedDeadline { get; set; }
    [LoadColumn(4)] public float AverageSeniorityYears { get; set; }
    [LoadColumn(5)] public float IsFirstProjectWithClient { get; set; }

    /// <summary>Set to ActualBudget for cost model, actual duration days for duration model.</summary>
    [LoadColumn(6)] public float Label { get; set; }
}
