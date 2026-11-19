using Microsoft.ML.Data;

namespace company_resource_manager.Server.Services.Estimation.ML;

public class ProjectEstimationOutput
{
    [ColumnName("Score")] public float Score { get; set; }
}
