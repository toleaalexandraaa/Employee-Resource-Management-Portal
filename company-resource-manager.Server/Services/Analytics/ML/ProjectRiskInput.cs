using Microsoft.ML.Data;

namespace company_resource_manager.Server.Services.Analytics.ML;

public class ProjectRiskInput
{
    public float BudgetBurnRate { get; set; }
    public float TimeBurnRate { get; set; }
    public float TimeElapsedRatio { get; set; }
    public float OverdueTaskCount { get; set; }
    public float TeamUtilizationRate { get; set; }
    public float WipViolationsCount { get; set; }
    public bool Label { get; set; }
}
