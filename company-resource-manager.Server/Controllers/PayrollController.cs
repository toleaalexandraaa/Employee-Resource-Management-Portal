using System.Security.Claims;
using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payroll;

    public PayrollController(IPayrollService payroll) => _payroll = payroll;

    [HttpGet]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _payroll.GetAllAsync(page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _payroll.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("generate")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> Generate([FromBody] GeneratePayrollRequest request)
    {
        try
        {
            var dto = await _payroll.GenerateAsync(request);
            return Created(string.Empty, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var approverUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = await _payroll.ApproveAsync(id, approverUserId);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}/pay")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> MarkAsPaid(Guid id)
    {
        var dto = await _payroll.MarkAsPaidAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:guid}/pdf")]
    public IActionResult GetPdf(Guid id)
        => StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PDF generation not implemented yet." });
}
