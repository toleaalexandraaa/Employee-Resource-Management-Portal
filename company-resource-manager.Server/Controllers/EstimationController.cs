using company_resource_manager.Server.DTOs.Estimation;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/estimation")]
[Authorize]
public class EstimationController : ControllerBase
{
    private readonly IEstimationService _estimationService;

    public EstimationController(IEstimationService estimationService)
    {
        _estimationService = estimationService;
    }

    [HttpPost("estimate")]
    public async Task<IActionResult> Estimate([FromBody] EstimateProjectRequest request)
    {
        var response = await _estimationService.EstimateAsync(request);
        return Ok(response);
    }

    [HttpPost("retrain")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Retrain(CancellationToken ct)
    {
        var response = await _estimationService.RetrainAsync(ct);
        return Ok(response);
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        var status = _estimationService.GetModelStatus();
        return Ok(status);
    }
}
