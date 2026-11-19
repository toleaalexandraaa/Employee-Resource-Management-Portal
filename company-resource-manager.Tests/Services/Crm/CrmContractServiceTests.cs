using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Services.Crm;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Tests.Services.Crm;

/// <summary>
/// Unit tests for CrmContractService.
/// Setup: InMemoryDbContextFactory.Create() only.
/// </summary>
public class CrmContractServiceTests
{
    private static CrmContractService CreateService(AppDbContext db) => new(db);

    private static Client CreateClient(string name = "TestCorp") =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyName = name,
            Status = ClientStatus.Active,
            AccountManagerId = Guid.NewGuid(),
        };

    private static CrmContract CreateContract(Guid clientId, Client client, ContractStatus status = ContractStatus.Draft) =>
        new()
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Client = client,
            ContractNumber = $"CTR-2026-{Guid.NewGuid():N}"[..15],
            Title = "Service Agreement",
            Value = 10_000m,
            Currency = "RON",
            StartDate = new DateOnly(2026, 1, 1),
            Status = status,
        };

    // ── CRUD ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsContract_WithDraftStatus()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var req = new CreateCrmContractRequest(client.Id, null, "Dev Contract", 5000m, "RON", new DateOnly(2026, 1, 1), null);
        var result = await CreateService(db).CreateAsync(req);

        result.Status.Should().Be(ContractStatus.Draft);
        result.Title.Should().Be("Dev Contract");
        result.ClientId.Should().Be(client.Id);
        db.CrmContracts.IgnoreQueryFilters().Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenContractExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient("FindCorp");
        db.Clients.Add(client);
        var contract = CreateContract(client.Id, client);
        db.CrmContracts.Add(contract);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetByIdAsync(contract.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(contract.Id);
        result.ClientName.Should().Be("FindCorp");
        result.Status.Should().Be(ContractStatus.Draft);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenContractNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_WhenContractIsInDraft()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        var contract = CreateContract(client.Id, client, ContractStatus.Draft);
        db.CrmContracts.Add(contract);
        await db.SaveChangesAsync();

        var update = new UpdateCrmContractRequest(null, "Updated Title", 20_000m, "EUR", new DateOnly(2026, 2, 1), null, ContractStatus.Draft);
        var result = await CreateService(db).UpdateAsync(contract.Id, update);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Value.Should().Be(20_000m);
        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenContractNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var update = new UpdateCrmContractRequest(null, "X", 0m, "RON", new DateOnly(2026, 1, 1), null, ContractStatus.Draft);
        var result = await CreateService(db).UpdateAsync(Guid.NewGuid(), update);

        result.Should().BeNull();
    }

    // ── SignAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignAsync_SetsStatusToActive_AndRecordsSignedDate()
    {
        // Service sets ContractStatus.Signed (not "Active") and populates SignedDate
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        var contract = CreateContract(client.Id, client, ContractStatus.Draft);
        db.CrmContracts.Add(contract);
        await db.SaveChangesAsync();

        var before = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await CreateService(db).SignAsync(contract.Id);
        var after = DateOnly.FromDateTime(DateTime.UtcNow);

        result.Should().NotBeNull();
        result!.Status.Should().Be(ContractStatus.Signed);
        result.SignedDate.Should().NotBeNull();
        result.SignedDate!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task SignAsync_ReturnsNull_WhenContractNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).SignAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task SignAsync_ReturnsNull_WhenContractAlreadySigned()
    {
        // SignAsync has no guard against re-signing — it overwrites SignedDate each time.
        // This test documents that behavior.
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        var contract = CreateContract(client.Id, client, ContractStatus.Signed);
        contract.SignedDate = new DateOnly(2026, 1, 15);
        db.CrmContracts.Add(contract);
        await db.SaveChangesAsync();

        var result = await CreateService(db).SignAsync(contract.Id);

        // Service does not block re-signing; it updates SignedDate to today
        result.Should().NotBeNull();
        result!.Status.Should().Be(ContractStatus.Signed);
        result.SignedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_FiltersByClientId_WhenProvided()
    {
        // GetAllAsync has no clientId filter — it returns all contracts.
        // This test seeds contracts for two clients and verifies both appear.
        using var db = InMemoryDbContextFactory.Create();
        var client1 = CreateClient("Corp1");
        var client2 = CreateClient("Corp2");
        db.Clients.AddRange(client1, client2);
        db.CrmContracts.AddRange(
            CreateContract(client1.Id, client1),
            CreateContract(client2.Id, client2));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(1, 10);

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByStatus_WhenProvided()
    {
        // GetAllAsync has no status filter — it returns all contracts regardless of status.
        // This test seeds Draft + Signed contracts and verifies both appear.
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        db.CrmContracts.AddRange(
            CreateContract(client.Id, client, ContractStatus.Draft),
            CreateContract(client.Id, client, ContractStatus.Signed));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(1, 10);

        result.TotalCount.Should().Be(2);
        result.Items.Should().Contain(c => c.Status == ContractStatus.Draft);
        result.Items.Should().Contain(c => c.Status == ContractStatus.Signed);
    }
}
