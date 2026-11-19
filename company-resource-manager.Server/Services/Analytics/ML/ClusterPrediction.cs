using Microsoft.ML.Data;

namespace company_resource_manager.Server.Services.Analytics.ML;

public class ClusterPrediction
{
    [ColumnName("PredictedLabel")]
    public uint PredictedClusterId { get; set; }
}
