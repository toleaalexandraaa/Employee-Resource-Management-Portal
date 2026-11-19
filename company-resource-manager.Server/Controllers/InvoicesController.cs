using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "Admin,AccountManager")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoices;

    public InvoicesController(IInvoiceService invoices) => _invoices = invoices;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _invoices.GetAllAsync(page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _invoices.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request)
    {
        try
        {
            var dto = await _invoices.CreateAsync(request);
            return Created(string.Empty, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("generate-from-timeentries")]
    public async Task<IActionResult> GenerateFromTimeEntries([FromBody] GenerateInvoiceFromTimeEntriesRequest request)
    {
        try
        {
            var dto = await _invoices.GenerateFromTimeEntriesAsync(request);
            return Created(string.Empty, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id)
    {
        var dto = await _invoices.SendAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request)
    {
        var dto = await _invoices.RecordPaymentAsync(id, request);
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
        var deleted = await _invoices.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
