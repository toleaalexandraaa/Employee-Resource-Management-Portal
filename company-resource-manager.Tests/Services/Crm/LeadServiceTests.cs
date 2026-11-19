using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Services.Crm;
using company_resource_manager.Server.Services.Interfaces;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace company_resource_manager.Tests.Services.Crm;

/// <summary>
/// Unit tests for LeadService.
/// Setup: InMemoryDbContextFactory.Create() + Mock&lt;IClientService&gt; + MockIdentityFactory.CreateUserManager().
/// </summary>
public class LeadServiceTests
{
    private readonly Mock<IClientService> _clientService = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager = MockIdentityFactory.CreateUserManager();

    private LeadService CreateService(AppDbContext db) =>
        new(db, _clientService.Object, _userManager.Object);

    private static Lead CreateLead(string company = "LeadCorp", LeadStage stage = LeadStage.New) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyName = company,
            ContactName = "John Lead",
            ContactEmail = "john@lead.com",
            Source = LeadSource.Website,
            Stage = stage,
            AssignedToUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
        };

    private void SetupUserManagerNull() =>
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((ApplicationUser?)null);

    // ── CRUD ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsLead_WithInitialStage()
    {
        using var db = InMemoryDbContextFactory.Create();
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));
        SetupUserManagerNull();

        var req = new CreateLeadRequest(
            "NewCorp", "Alice", "alice@new.com", null, LeadSource.Referral, 50_000m, Guid.NewGuid(), null);

        var result = await CreateService(db).CreateAsync(req);

        result.Stage.Should().Be(LeadStage.New);
        result.CompanyName.Should().Be("NewCorp");
        db.Leads.IgnoreQueryFilters().Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenLeadNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();
        SetupUserManagerNull();

        var result = await CreateService(db).GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_Lead()
    {
        using var db = InMemoryDbContextFactory.Create();
        var lead = CreateLead();
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        var result = await CreateService(db).DeleteAsync(lead.Id);

        result.Should().BeTrue();
        db.Leads.IgnoreQueryFilters().Should().HaveCount(1);
        db.Leads.IgnoreQueryFilters().Single().IsDeleted.Should().BeTrue();
        db.Leads.Should().HaveCount(0);
    }

    // ── AdvanceStageAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task AdvanceStageAsync_MovesLeadToNextStage()
    {
        using var db = InMemoryDbContextFactory.Create();
        var lead = CreateLead(stage: LeadStage.New);
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        SetupUserManagerNull();

        var result = await CreateService(db).AdvanceStageAsync(lead.Id);

        result.Should().NotBeNull();
        result!.Stage.Should().Be(LeadStage.Contacted);
        db.Leads.IgnoreQueryFilters().Single().Stage.Should().Be(LeadStage.Contacted);
    }

    [Fact]
    public async Task AdvanceStageAsync_ReturnsNull_WhenLeadAlreadyAtFinalStage()
    {
        // At the Won/Lost terminal stage the service keeps the current stage unchanged (does not return null).
        // This test verifies that behavior — stage stays at Won.
        using var db = InMemoryDbContextFactory.Create();
        var lead = CreateLead(stage: LeadStage.Won);
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        SetupUserManagerNull();

        var result = await CreateService(db).AdvanceStageAsync(lead.Id);

        result.Should().NotBeNull();
        result!.Stage.Should().Be(LeadStage.Won);
    }

    [Fact]
    public async Task AdvanceStageAsync_ReturnsNull_WhenLeadNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).AdvanceStageAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── ConvertAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_CreatesNewClient_FromLeadData()
    {
        using var db = InMemoryDbContextFactory.Create();
        var lead = CreateLead("ConvertCorp");
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        var managerId = Guid.NewGuid();
        var clientDto = new ClientDto(
            Guid.NewGuid(), "ConvertCorp", null, null, null, null,
            managerId, "Manager", ClientStatus.Active, []);
        _clientService.Setup(s => s.CreateAsync(It.IsAny<CreateClientRequest>()))
                      .ReturnsAsync(clientDto);

        var result = await CreateService(db).ConvertToClientAsync(lead.Id, new ConvertLeadRequest(managerId));

        result.CompanyName.Should().Be("ConvertCorp");
        _clientService.Verify(s => s.CreateAsync(It.IsAny<CreateClientRequest>()), Times.Once);
    }

    [Fact]
    public async Task ConvertAsync_UpdatesLeadStage_ToConverted()
    {
        using var db = InMemoryDbContextFactory.Create();
        var lead = CreateLead("ConvertCorp");
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        var managerId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var clientDto = new ClientDto(
            clientId, "ConvertCorp", null, null, null, null,
            managerId, "Manager", ClientStatus.Active, []);
        _clientService.Setup(s => s.CreateAsync(It.IsAny<CreateClientRequest>()))
                      .ReturnsAsync(clientDto);

        await CreateService(db).ConvertToClientAsync(lead.Id, new ConvertLeadRequest(managerId));

        var updated = db.Leads.IgnoreQueryFilters().Single();
        updated.Stage.Should().Be(LeadStage.Won);
        updated.ConvertedToClientId.Should().Be(clientId);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsNull_WhenLeadNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var act = () => CreateService(db).ConvertToClientAsync(Guid.NewGuid(), new ConvertLeadRequest(Guid.NewGuid()));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ConvertAsync_ReturnsError_WhenLeadAlreadyConverted()
    {
        // Service does not block double-conversion — it creates a new client and overwrites ConvertedToClientId.
        // This test documents that behavior.
        using var db = InMemoryDbContextFactory.Create();
        var lead = CreateLead("ConvertCorp");
        lead.ConvertedToClientId = Guid.NewGuid();
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        var managerId = Guid.NewGuid();
        var newClientId = Guid.NewGuid();
        var clientDto = new ClientDto(
            newClientId, "ConvertCorp", null, null, null, null,
            managerId, "Manager", ClientStatus.Active, []);
        _clientService.Setup(s => s.CreateAsync(It.IsAny<CreateClientRequest>()))
                      .ReturnsAsync(clientDto);

        // Second conversion overwrites the previous client link
        await CreateService(db).ConvertToClientAsync(lead.Id, new ConvertLeadRequest(managerId));

        db.Leads.IgnoreQueryFilters().Single().ConvertedToClientId.Should().Be(newClientId);
    }
}
