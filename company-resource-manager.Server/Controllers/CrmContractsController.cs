using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/crm-contracts")]
[Authorize(Roles = "Admin,AccountManager")]
public class CrmContractsController : ControllerBase
{
    private readonly ICrmContractService _contracts;

    public CrmContractsController(ICrmContractService contracts) => _contracts = contracts;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _contracts.GetAllAsync(page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _contracts.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCrmContractRequest request)
    {
        var dto = await _contracts.CreateAsync(request);
        return Created(string.Empty, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCrmContractRequest request)
    {
        var dto = await _contracts.UpdateAsync(id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}/sign")]
    public async Task<IActionResult> Sign(Guid id)
    {
        var dto = await _contracts.SignAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:guid}/pdf")]
    public IActionResult GetPdf(Guid id)
        => StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PDF generation not implemented yet." });

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _contracts.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
