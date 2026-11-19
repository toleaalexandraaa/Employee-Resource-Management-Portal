using System.Security.Claims;
using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employees;

    public EmployeesController(IEmployeeService employees) => _employees = employees;

    [HttpGet]
    [Authorize(Roles = "Admin,HrManager,ProjectManager,AccountManager")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _employees.GetAllAsync(page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _employees.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
    {
        var result = await _employees.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Employee.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        var dto = await _employees.UpdateAsync(id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _employees.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/documents")]
    [Authorize(Roles = "Admin,HrManager")]
    public IActionResult GetDocuments(Guid id)
        => StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "File storage not implemented yet." });
}
