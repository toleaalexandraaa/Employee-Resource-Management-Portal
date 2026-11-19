using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Analytics;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Analytics.ML;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using TaskStatus = company_resource_manager.Server.Models.Proj.TaskStatus;

namespace company_resource_manager.Server.Services.Analytics;

public class ClusteringService : IClusteringService
{
    private const int MinEmployees = 3;
    private const int MinClients = 3;
    private const int KClusters = 3;

    private static readonly string[] EmployeeFeatures =
    [
        nameof(EmployeeClusterInput.TasksCompleted),
        nameof(EmployeeClusterInput.HoursLogged),
        nameof(EmployeeClusterInput.HoursVsEstimatedRatio),
        nameof(EmployeeClusterInput.ProjectsActive),
        nameof(EmployeeClusterInput.SeniorityYears),
    ];

    private static readonly string[] ClientFeatures =
    [
        nameof(ClientSegmentInput.TotalRevenue),
        nameof(ClientSegmentInput.ProjectCount),
        nameof(ClientSegmentInput.AvgPaymentDelayDays),
        nameof(ClientSegmentInput.OpenInvoiceAmount),
        nameof(ClientSegmentInput.LeadCount),
    ];

    private readonly AppDbContext _db;
    private readonly ILogger<ClusteringService> _logger;

    public ClusteringService(AppDbContext db, ILogger<ClusteringService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Employee Clustering ───────────────────────────────────────────────────

    public async Task<EmployeeClustersDto> GetEmployeeClustersAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-90);

        var employees = await _db.Employees
            .AsNoTracking()
            .Where(e => e.IsActive)
            .ToListAsync(ct);

        if (employees.Count < MinEmployees)
        {
            return new EmployeeClustersDto(
                [],
                false,
                $"Insufficient data: {employees.Count} active employees (minimum {MinEmployees} required for clustering).");
        }

        // Time entries in last 90 days grouped by employee
        var timeEntries = await _db.TimeEntries
            .AsNoTracking()
            .Where(te => te.Date >= DateOnly.FromDateTime(cutoff))
            .ToListAsync(ct);

        var hoursByEmployee = timeEntries
            .GroupBy(te => te.EmployeeId)
            .ToDictionary(g => g.Key, g => (float)g.Sum(te => te.HoursLogged));

        // Done tasks in last 90 days grouped by assigned employee
        var doneTasks = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.Status == TaskStatus.Done && t.UpdatedAt >= cutoff)
            .ToListAsync(ct);

        var tasksByEmployee = doneTasks
            .Where(t => t.AssignedToEmployeeId.HasValue)
            .GroupBy(t => t.AssignedToEmployeeId!.Value)
            .ToDictionary(g => g.Key, g => (float)g.Count());

        // Estimated hours for tasks in projects where employees are members
        var memberProjects = await _db.ProjectMembers
            .AsNoTracking()
            .Include(pm => pm.Project)
                .ThenInclude(p => p.Tasks)
            .Where(pm => pm.Project.Status == ProjectStatus.Active)
            .ToListAsync(ct);

        var estimatedHoursByEmployee = memberProjects
            .GroupBy(pm => pm.EmployeeId)
            .ToDictionary(g => g.Key,
                g => (float)g.SelectMany(pm => pm.Project.Tasks).Sum(t => t.EstimatedHours));

        var activeProjectsByEmployee = memberProjects
            .GroupBy(pm => pm.EmployeeId)
            .ToDictionary(g => g.Key, g => (float)g.Select(pm => pm.ProjectId).Distinct().Count());

        // Build inputs
        var inputs = employees.Select(e =>
        {
            var hours = hoursByEmployee.GetValueOrDefault(e.Id, 0f);
            var tasks = tasksByEmployee.GetValueOrDefault(e.Id, 0f);
            var estHours = estimatedHoursByEmployee.GetValueOrDefault(e.Id, 0f);
            var ratio = estHours > 0 ? hours / estHours : (hours > 0 ? 2f : 1f);

            return new EmployeeClusterInput
            {
                TasksCompleted = tasks,
                HoursLogged = hours,
                HoursVsEstimatedRatio = Math.Min(ratio, 3f),
                ProjectsActive = activeProjectsByEmployee.GetValueOrDefault(e.Id, 0f),
                SeniorityYears = e.SeniorityYears,
            };
        }).ToList();

        var mlContext = new MLContext(seed: 42);
        var dataView = mlContext.Data.LoadFromEnumerable(inputs);

        var pipeline = mlContext.Transforms
            .Concatenate("Features", EmployeeFeatures)
            .Append(mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(mlContext.Clustering.Trainers.KMeans(
                featureColumnName: "Features",
                numberOfClusters: KClusters));

        ITransformer model;
        try
        {
            model = pipeline.Fit(dataView);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "KMeans training failed for employee clusters.");
            return new EmployeeClustersDto([], false, "Clustering model failed to train.");
        }

        var predictions = model.Transform(dataView);
        var clusterIds = mlContext.Data
            .CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false)
            .Select(p => (int)p.PredictedClusterId)
            .ToList();

        // Compute mean (hours + tasks) per cluster to assign labels
        var clusterScores = Enumerable.Range(0, employees.Count)
            .GroupBy(i => clusterIds[i])
            .ToDictionary(
                g => g.Key,
                g => g.Average(i => inputs[i].HoursLogged + inputs[i].TasksCompleted * 8f));

        var sortedClusters = clusterScores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();

        var clusterLabels = new Dictionary<int, string>();
        if (sortedClusters.Count >= 1) clusterLabels[sortedClusters[0]] = "High Performer";
        if (sortedClusters.Count >= 2) clusterLabels[sortedClusters[1]] = "Standard";
        if (sortedClusters.Count >= 3) clusterLabels[sortedClusters[2]] = "Low Utilisation";

        var points = employees.Select((e, i) =>
        {
            var cluster = clusterIds[i];
            return new EmployeeClusterPoint(
                e.Id,
                $"{e.FirstName} {e.LastName}",
                inputs[i].HoursLogged,
                inputs[i].TasksCompleted,
                inputs[i].HoursVsEstimatedRatio,
                inputs[i].ProjectsActive,
                inputs[i].SeniorityYears,
                cluster,
                clusterLabels.GetValueOrDefault(cluster, "Standard"));
        }).ToList();

        return new EmployeeClustersDto(points, true, $"Clustered {employees.Count} employees (last 90 days).");
    }

    // ── Client Segmentation ───────────────────────────────────────────────────

    public async Task<ClientSegmentsDto> GetClientSegmentsAsync(CancellationToken ct = default)
    {
        var clients = await _db.Clients
            .AsNoTracking()
            .ToListAsync(ct);

        if (clients.Count < MinClients)
        {
            return new ClientSegmentsDto(
                [],
                false,
                $"Insufficient data: {clients.Count} clients (minimum {MinClients} required for clustering).");
        }

        var invoices = await _db.Invoices
            .AsNoTracking()
            .ToListAsync(ct);

        var projects = await _db.Projects
            .AsNoTracking()
            .Where(p => p.ClientId.HasValue)
            .ToListAsync(ct);

        var convertedLeads = await _db.Leads
            .AsNoTracking()
            .Where(l => l.ConvertedToClientId.HasValue)
            .ToListAsync(ct);

        var projectsByClient = projects
            .GroupBy(p => p.ClientId!.Value)
            .ToDictionary(g => g.Key, g => (float)g.Count());

        var leadsByClient = convertedLeads
            .GroupBy(l => l.ConvertedToClientId!.Value)
            .ToDictionary(g => g.Key, g => (float)g.Count());

        var paidInvoices = invoices.Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt.HasValue).ToList();
        var unpaidStatuses = new[] { InvoiceStatus.Draft, InvoiceStatus.Sent, InvoiceStatus.PartiallyPaid, InvoiceStatus.Overdue };
        var unpaidInvoices = invoices.Where(i => unpaidStatuses.Contains(i.Status)).ToList();

        var revenueByClient = paidInvoices
            .GroupBy(i => i.ClientId)
            .ToDictionary(g => g.Key, g => (float)g.Sum(i => i.PaidAmount));

        var avgDelayByClient = paidInvoices
            .GroupBy(i => i.ClientId)
            .ToDictionary(g => g.Key, g =>
            {
                var delays = g
                    .Where(i => i.PaidAt.HasValue)
                    .Select(i =>
                    {
                        var paid = DateOnly.FromDateTime(i.PaidAt!.Value);
                        return (float)(paid.DayNumber - i.DueDate.DayNumber);
                    })
                    .ToList();
                return delays.Count > 0 ? delays.Average() : 0f;
            });

        var openByClient = unpaidInvoices
            .GroupBy(i => i.ClientId)
            .ToDictionary(g => g.Key, g => (float)g.Sum(i => i.TotalAmount - i.PaidAmount));

        var inputs = clients.Select(c => new ClientSegmentInput
        {
            TotalRevenue = revenueByClient.GetValueOrDefault(c.Id, 0f),
            ProjectCount = projectsByClient.GetValueOrDefault(c.Id, 0f),
            AvgPaymentDelayDays = avgDelayByClient.GetValueOrDefault(c.Id, 0f),
            OpenInvoiceAmount = openByClient.GetValueOrDefault(c.Id, 0f),
            LeadCount = leadsByClient.GetValueOrDefault(c.Id, 0f),
        }).ToList();

        var mlContext = new MLContext(seed: 42);
        var dataView = mlContext.Data.LoadFromEnumerable(inputs);

        var pipeline = mlContext.Transforms
            .Concatenate("Features", ClientFeatures)
            .Append(mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(mlContext.Clustering.Trainers.KMeans(
                featureColumnName: "Features",
                numberOfClusters: Math.Min(KClusters, clients.Count)));

        ITransformer model;
        try
        {
            model = pipeline.Fit(dataView);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "KMeans training failed for client segments.");
            return new ClientSegmentsDto([], false, "Clustering model failed to train.");
        }

        var predictions = model.Transform(dataView);
        var clusterIds = mlContext.Data
            .CreateEnumerable<ClusterPrediction>(predictions, reuseRowObject: false)
            .Select(p => (int)p.PredictedClusterId)
            .ToList();

        // Assign labels: rank by (total_revenue - open_invoice_amount + avg_payment_delay penalty)
        var clusterScores = Enumerable.Range(0, clients.Count)
            .GroupBy(i => clusterIds[i])
            .ToDictionary(
                g => g.Key,
                g => g.Average(i => inputs[i].TotalRevenue - inputs[i].AvgPaymentDelayDays * 100f - inputs[i].OpenInvoiceAmount * 0.5f));

        var sortedClusters = clusterScores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();

        var clusterLabels = new Dictionary<int, string>();
        if (sortedClusters.Count >= 1) clusterLabels[sortedClusters[0]] = "Key Account";
        if (sortedClusters.Count >= 2) clusterLabels[sortedClusters[1]] = "Growth Client";
        if (sortedClusters.Count >= 3) clusterLabels[sortedClusters[2]] = "At Risk";

        var points = clients.Select((c, i) =>
        {
            var cluster = clusterIds[i];
            return new ClientSegmentPoint(
                c.Id,
                c.CompanyName,
                inputs[i].TotalRevenue,
                inputs[i].ProjectCount,
                inputs[i].AvgPaymentDelayDays,
                inputs[i].OpenInvoiceAmount,
                inputs[i].LeadCount,
                cluster,
                clusterLabels.GetValueOrDefault(cluster, "Growth Client"));
        }).ToList();

        return new ClientSegmentsDto(points, true, $"Segmented {clients.Count} clients.");
    }
}
