namespace company_resource_manager.Server.DTOs.Analytics;

// ── Leads ─────────────────────────────────────────────────────────────────────

public record LeadFunnelStageDto(string Stage, int Count, decimal TotalValue);

public record CrmLeadsDto(
    IReadOnlyList<LeadFunnelStageDto> Funnel,
    int WonCount,
    int LostCount,
    IReadOnlyList<LabelValueDto> LeadsBySource,
    IReadOnlyList<LabelValueDto> AvgDealSizeBySource,
    IReadOnlyList<LabelValueDto> AvgConversionDaysBySource
);

// ── Revenue ───────────────────────────────────────────────────────────────────

public record InvoiceAgingBucketDto(string Bucket, int Count, decimal TotalAmount);

public record ReceivablesByStatusDto(string Status, decimal Amount);

public record CrmRevenueDto(
    IReadOnlyList<MonthlyValueDto> RevenueLast12Months,
    IReadOnlyList<LabelValueDto> RevenueByClient,
    IReadOnlyList<LabelValueDto> RevenueByIndustry,
    IReadOnlyList<InvoiceAgingBucketDto> AgingBuckets,
    decimal TotalOutstanding,
    IReadOnlyList<ReceivablesByStatusDto> OutstandingByStatus
);

// ── Clients ───────────────────────────────────────────────────────────────────

public record TopClientDto(string ClientName, int ProjectCount, decimal TotalInvoiced, decimal TotalPaid);

public record CrmClientsDto(
    IReadOnlyList<LabelValueDto> ClientsByStatus,
    IReadOnlyList<LabelValueDto> ClientsByIndustry,
    IReadOnlyList<MonthlyValueDto> AcquisitionByQuarter,
    IReadOnlyList<TopClientDto> TopClients
);
