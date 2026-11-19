using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/employment-contracts")]
[Authorize(Roles = "Admin,HrManager")]
public class EmploymentContractsController : ControllerBase
{
    private readonly IEmploymentContractService _contracts;

    public EmploymentContractsController(IEmploymentContractService contracts) => _contracts = contracts;

    [HttpGet("employee/{employeeId:guid}")]
    public async Task<IActionResult> GetByEmployee(Guid employeeId)
        => Ok(await _contracts.GetByEmployeeAsync(employeeId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request)
    {
        var dto = await _contracts.CreateAsync(request);
        return Created(string.Empty, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContractRequest request)
    {
        var dto = await _contracts.UpdateAsync(id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _contracts.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
