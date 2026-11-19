using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Roles = "Admin,AccountManager,ProjectManager")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clients;

    public ClientsController(IClientService clients) => _clients = clients;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _clients.GetAllAsync(page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _clients.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
    {
        var dto = await _clients.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest request)
    {
        var dto = await _clients.UpdateAsync(id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _clients.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/contacts")]
    public async Task<IActionResult> GetContacts(Guid id)
        => Ok(await _clients.GetContactsAsync(id));

    [HttpPost("{id:guid}/contacts")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> AddContact(Guid id, [FromBody] CreateClientContactRequest request)
    {
        var dto = await _clients.AddContactAsync(id, request);
        return Created(string.Empty, dto);
    }

    [HttpPut("{id:guid}/contacts/{contactId:guid}")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> UpdateContact(Guid id, Guid contactId, [FromBody] UpdateClientContactRequest request)
    {
        var dto = await _clients.UpdateContactAsync(id, contactId, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> RemoveContact(Guid id, Guid contactId)
    {
        var removed = await _clients.RemoveContactAsync(id, contactId);
        return removed ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var dto = await _clients.GetHistoryAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }
}
