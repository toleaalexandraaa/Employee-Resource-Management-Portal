using Microsoft.ML.Data;

namespace company_resource_manager.Server.Services.Analytics.ML;

public class ClientSegmentInput
{
    public float TotalRevenue { get; set; }
    public float ProjectCount { get; set; }
    public float AvgPaymentDelayDays { get; set; }
    public float OpenInvoiceAmount { get; set; }
    public float LeadCount { get; set; }
}
