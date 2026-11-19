using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Crm;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace company_resource_manager.Tests.Services.Crm;

/// <summary>
/// Unit tests for ClientService.
/// Setup: InMemoryDbContextFactory.Create() + MockIdentityFactory.CreateUserManager().
/// </summary>
public class ClientServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager = MockIdentityFactory.CreateUserManager();

    private ClientService CreateService(AppDbContext db) => new(db, _userManager.Object);

    private static Client CreateClient(string name = "Acme Corp") =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyName = name,
            Industry = "Tech",
            Country = "RO",
            Status = ClientStatus.Active,
            AccountManagerId = Guid.NewGuid(),
        };

    private void SetupUserManagerNull() =>
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((ApplicationUser?)null);

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaged_Results()
    {
        using var db = InMemoryDbContextFactory.Create();
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        for (int i = 0; i < 12; i++)
            db.Clients.Add(CreateClient($"Client {i:D2}"));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(2, 10);

        result.TotalCount.Should().Be(12);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedClients()
    {
        using var db = InMemoryDbContextFactory.Create();
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        var active = CreateClient("ActiveCo");
        var toDelete = CreateClient("DeletedCo");
        db.Clients.AddRange(active, toDelete);
        await db.SaveChangesAsync();

        db.Clients.Remove(toDelete);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(1, 10);

        result.TotalCount.Should().Be(1);
        result.Items.Single().CompanyName.Should().Be("ActiveCo");
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenClientExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient("TestCorp");
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        SetupUserManagerNull();

        var result = await CreateService(db).GetByIdAsync(client.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(client.Id);
        result.CompanyName.Should().Be("TestCorp");
        result.Status.Should().Be(ClientStatus.Active);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenClientNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();
        SetupUserManagerNull();

        var result = await CreateService(db).GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsClient_WithCorrectFields()
    {
        using var db = InMemoryDbContextFactory.Create();
        SetupUserManagerNull();

        var managerId = Guid.NewGuid();
        var req = new CreateClientRequest("NewCorp", "Finance", "newcorp.com", "123 Main St", "RO", managerId);

        var result = await CreateService(db).CreateAsync(req);

        result.CompanyName.Should().Be("NewCorp");
        result.Industry.Should().Be("Finance");
        result.AccountManagerId.Should().Be(managerId);
        db.Clients.IgnoreQueryFilters().Should().HaveCount(1);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesFields_WhenClientExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient("OldName");
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        SetupUserManagerNull();

        var newManagerId = Guid.NewGuid();
        var update = new UpdateClientRequest("NewName", "Banking", null, null, "DE", newManagerId, ClientStatus.Inactive);

        var result = await CreateService(db).UpdateAsync(client.Id, update);

        result.Should().NotBeNull();
        result!.CompanyName.Should().Be("NewName");
        result.Industry.Should().Be("Banking");
        result.Status.Should().Be(ClientStatus.Inactive);
        result.AccountManagerId.Should().Be(newManagerId);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenClientNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();
        SetupUserManagerNull();

        var update = new UpdateClientRequest("X", null, null, null, null, Guid.NewGuid(), ClientStatus.Active);

        var result = await CreateService(db).UpdateAsync(Guid.NewGuid(), update);

        result.Should().BeNull();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletes_Client()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient("ToDelete");
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var result = await CreateService(db).DeleteAsync(client.Id);

        result.Should().BeTrue();
        db.Clients.IgnoreQueryFilters().Should().HaveCount(1);
        db.Clients.IgnoreQueryFilters().Single().IsDeleted.Should().BeTrue();
        db.Clients.Should().HaveCount(0);
    }

    // ── Contacts ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddContactAsync_AddsContactToClient()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var req = new CreateClientContactRequest("Alice Smith", "CEO", "alice@corp.com", "+40700000000");
        var result = await CreateService(db).AddContactAsync(client.Id, req);

        result.FullName.Should().Be("Alice Smith");
        result.ClientId.Should().Be(client.Id);
        db.ClientContacts.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateContactAsync_UpdatesContactFields()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        var contact = new ClientContact
        {
            Id = Guid.NewGuid(), ClientId = client.Id, FullName = "Old Name", Email = "old@corp.com",
        };
        db.ClientContacts.Add(contact);
        await db.SaveChangesAsync();

        var update = new UpdateClientContactRequest("New Name", "CFO", "new@corp.com", null, true);
        var result = await CreateService(db).UpdateContactAsync(client.Id, contact.Id, update);

        result.Should().NotBeNull();
        result!.FullName.Should().Be("New Name");
        result.JobTitle.Should().Be("CFO");
        result.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteContactAsync_RemovesContact_WhenExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        var contact = new ClientContact
        {
            Id = Guid.NewGuid(), ClientId = client.Id, FullName = "Alice", Email = "a@b.com",
        };
        db.ClientContacts.Add(contact);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RemoveContactAsync(client.Id, contact.Id);

        result.Should().BeTrue();
        db.ClientContacts.Should().HaveCount(0);
    }

    [Fact]
    public async Task GetContactsAsync_ReturnsOnlyContactsForClient()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client1 = CreateClient("Corp1");
        var client2 = CreateClient("Corp2");
        db.Clients.AddRange(client1, client2);
        db.ClientContacts.AddRange(
            new ClientContact { Id = Guid.NewGuid(), ClientId = client1.Id, FullName = "C1-Contact1", Email = "c1a@corp.com" },
            new ClientContact { Id = Guid.NewGuid(), ClientId = client1.Id, FullName = "C1-Contact2", Email = "c1b@corp.com" },
            new ClientContact { Id = Guid.NewGuid(), ClientId = client2.Id, FullName = "C2-Contact1", Email = "c2a@corp.com" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetContactsAsync(client1.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.ClientId.Should().Be(client1.Id));
    }

    // ── History ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_ReturnsProjectsAndInvoicesForClient()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient("HistoryCorp");
        db.Clients.Add(client);

        db.Projects.AddRange(
            new Project { Id = Guid.NewGuid(), ClientId = client.Id, Name = "P1", Status = ProjectStatus.Active, StartDate = new DateOnly(2026, 1, 1) },
            new Project { Id = Guid.NewGuid(), ClientId = client.Id, Name = "P2", Status = ProjectStatus.Completed, StartDate = new DateOnly(2025, 1, 1) });

        db.Invoices.AddRange(
            new Invoice { Id = Guid.NewGuid(), ClientId = client.Id, InvoiceNumber = "INV-2026-001", TotalAmount = 500m, Currency = "RON", Status = InvoiceStatus.Sent, IssueDate = new DateOnly(2026, 1, 1), DueDate = new DateOnly(2026, 2, 1) },
            new Invoice { Id = Guid.NewGuid(), ClientId = client.Id, InvoiceNumber = "INV-2026-002", TotalAmount = 800m, Currency = "RON", Status = InvoiceStatus.Paid, IssueDate = new DateOnly(2026, 2, 1), DueDate = new DateOnly(2026, 3, 1) },
            new Invoice { Id = Guid.NewGuid(), ClientId = client.Id, InvoiceNumber = "INV-2026-003", TotalAmount = 200m, Currency = "RON", Status = InvoiceStatus.Draft, IssueDate = new DateOnly(2026, 3, 1), DueDate = new DateOnly(2026, 4, 1) });

        await db.SaveChangesAsync();

        var result = await CreateService(db).GetHistoryAsync(client.Id);

        result.Should().NotBeNull();
        result!.Projects.Should().HaveCount(2);
        result.Invoices.Should().HaveCount(3);
    }
}
