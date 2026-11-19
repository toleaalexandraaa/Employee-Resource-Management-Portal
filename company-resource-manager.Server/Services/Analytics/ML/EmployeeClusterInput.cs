using Microsoft.ML.Data;

namespace company_resource_manager.Server.Services.Analytics.ML;

public class EmployeeClusterInput
{
    public float TasksCompleted { get; set; }
    public float HoursLogged { get; set; }
    public float HoursVsEstimatedRatio { get; set; }
    public float ProjectsActive { get; set; }
    public float SeniorityYears { get; set; }
}
