using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IOverviewAnalyticsService _overview;
    private readonly IHrAnalyticsService _hr;
    private readonly IProjectAnalyticsService _projects;
    private readonly ICrmAnalyticsService _crm;
    private readonly IClusteringService _clustering;
    private readonly IRiskScoringService _risk;

    public AnalyticsController(
        IOverviewAnalyticsService overview,
        IHrAnalyticsService hr,
        IProjectAnalyticsService projects,
        ICrmAnalyticsService crm,
        IClusteringService clustering,
        IRiskScoringService risk)
    {
        _overview = overview;
        _hr = hr;
        _projects = projects;
        _crm = crm;
        _clustering = clustering;
        _risk = risk;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
        => Ok(await _overview.GetOverviewAsync());

    [HttpGet("hr/workforce")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> GetHrWorkforce()
        => Ok(await _hr.GetWorkforceAsync());

    [HttpGet("hr/leave")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> GetHrLeave()
        => Ok(await _hr.GetLeaveAsync());

    [HttpGet("hr/payroll")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> GetHrPayroll()
        => Ok(await _hr.GetPayrollAsync());

    [HttpGet("projects/health")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> GetProjectHealth()
        => Ok(await _projects.GetHealthAsync());

    [HttpGet("projects/time")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> GetProjectTime()
        => Ok(await _projects.GetTimeAsync());

    [HttpGet("projects/tasks")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> GetProjectTasks()
        => Ok(await _projects.GetTasksAsync());

    [HttpGet("crm/leads")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> GetCrmLeads()
        => Ok(await _crm.GetLeadsAsync());

    [HttpGet("crm/revenue")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> GetCrmRevenue()
        => Ok(await _crm.GetRevenueAsync());

    [HttpGet("crm/clients")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> GetCrmClients()
        => Ok(await _crm.GetClientsAsync());

    // ── ML Clustering ─────────────────────────────────────────────────────────

    [HttpGet("ml/employee-clusters")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> GetEmployeeClusters(CancellationToken ct)
        => Ok(await _clustering.GetEmployeeClustersAsync(ct));

    [HttpGet("ml/client-segments")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> GetClientSegments(CancellationToken ct)
        => Ok(await _clustering.GetClientSegmentsAsync(ct));

    [HttpGet("ml/project-risks")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> GetProjectRisks(CancellationToken ct)
        => Ok(await _risk.GetProjectRisksAsync(ct));

    [HttpGet("projects/{id:guid}/risk")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> GetProjectRisk(Guid id, CancellationToken ct)
    {
        var result = await _risk.GetProjectRiskAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
