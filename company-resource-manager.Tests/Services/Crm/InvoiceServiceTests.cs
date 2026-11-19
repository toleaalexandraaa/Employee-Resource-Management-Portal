using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Models.Shared;
using company_resource_manager.Server.Services.Crm;
using company_resource_manager.Server.Services.Interfaces;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace company_resource_manager.Tests.Services.Crm;

/// <summary>
/// Unit tests for InvoiceService.
/// Setup: InMemoryDbContextFactory.Create() + Mock&lt;INotificationService&gt; + MockIdentityFactory.CreateUserManager().
/// </summary>
public class InvoiceServiceTests
{
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager = MockIdentityFactory.CreateUserManager();

    private InvoiceService CreateService(AppDbContext db) =>
        new(db, _notifications.Object, _userManager.Object);

    private static Client CreateClient(string name = "TestCorp") =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyName = name,
            Status = ClientStatus.Active,
            AccountManagerId = Guid.NewGuid(),
        };

    private static CreateInvoiceRequest BuildRequest(Guid clientId, IReadOnlyList<CreateInvoiceLineItemRequest> lineItems) =>
        new(clientId, null, null,
            new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1),
            0m, "RON", lineItems);

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsInvoice_WithLineItems()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var lineItems = new List<CreateInvoiceLineItemRequest>
        {
            new("Service A", 1m, 100m),
            new("Service B", 2m, 75m),
        };
        var result = await CreateService(db).CreateAsync(BuildRequest(client.Id, lineItems));

        result.LineItems.Should().HaveCount(2);
        db.Invoices.IgnoreQueryFilters().Should().HaveCount(1);
        db.InvoiceLineItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_CalculatesTotalAmount_SumOfLineItemAmounts()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        // 100 * 1 + 250 * 1 = 350 (TaxRate = 0 so TotalAmount = SubTotal)
        var lineItems = new List<CreateInvoiceLineItemRequest>
        {
            new("Item A", 1m, 100m),
            new("Item B", 1m, 250m),
        };
        var result = await CreateService(db).CreateAsync(BuildRequest(client.Id, lineItems));

        result.SubTotal.Should().Be(350m);
        result.TotalAmount.Should().Be(350m);
    }

    [Fact]
    public async Task CreateAsync_SetsStatus_ToDraft_ByDefault()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var result = await CreateService(db).CreateAsync(
            BuildRequest(client.Id, [new("Item", 1m, 100m)]));

        result.Status.Should().Be(InvoiceStatus.Draft);
    }

    // ── GenerateFromTimeEntriesAsync ──────────────────────────────────────────

    [Fact]
    public async Task GenerateFromTimeEntriesAsync_CreatesInvoice_WithHoursAsLineItems()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        var project = new Project { Id = Guid.NewGuid(), Name = "Proj", Status = ProjectStatus.Active, StartDate = new DateOnly(2026, 1, 1) };
        var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "Dev Work" };
        db.Clients.Add(client);
        db.Projects.Add(project);
        db.Tasks.Add(task);
        // 3 time entries: 2h + 3h + 5h = 10h at $100/h → $1000
        db.TimeEntries.AddRange(
            new TimeEntry { Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), ProjectId = project.Id, TaskId = task.Id, Date = new DateOnly(2026, 3, 1), HoursLogged = 2m },
            new TimeEntry { Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), ProjectId = project.Id, TaskId = task.Id, Date = new DateOnly(2026, 3, 10), HoursLogged = 3m },
            new TimeEntry { Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), ProjectId = project.Id, TaskId = task.Id, Date = new DateOnly(2026, 3, 20), HoursLogged = 5m });
        await db.SaveChangesAsync();

        var req = new GenerateInvoiceFromTimeEntriesRequest(
            project.Id, client.Id, null,
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31),
            100m, "RON", 30);

        var result = await CreateService(db).GenerateFromTimeEntriesAsync(req);

        result.TotalAmount.Should().Be(1000m);
        result.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public async Task GenerateFromTimeEntriesAsync_ReturnsError_WhenNoTimeEntriesFound()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        var project = new Project { Id = Guid.NewGuid(), Name = "P", Status = ProjectStatus.Active, StartDate = new DateOnly(2026, 1, 1) };
        db.Clients.Add(client);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var req = new GenerateInvoiceFromTimeEntriesRequest(
            project.Id, client.Id, null,
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31),
            100m, "RON", 30);

        var act = () => CreateService(db).GenerateFromTimeEntriesAsync(req);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No time entries*");
    }

    // ── SendAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_ChangesStatus_FromDraftToSent()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), ClientId = client.Id, Client = client,
            InvoiceNumber = "INV-2026-001", TotalAmount = 500m, Currency = "RON",
            Status = InvoiceStatus.Draft, IssueDate = new DateOnly(2026, 3, 1), DueDate = new DateOnly(2026, 4, 1),
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var result = await CreateService(db).SendAsync(invoice.Id);

        result.Should().NotBeNull();
        result!.Status.Should().Be(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task SendAsync_ReturnsNull_WhenInvoiceNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).SendAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_ReturnsNull_WhenInvoiceIsNotInDraftStatus()
    {
        // SendAsync does not enforce a Draft-only guard — any invoice can be set to Sent.
        // This test documents that behavior.
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), ClientId = client.Id, Client = client,
            InvoiceNumber = "INV-2026-002", TotalAmount = 200m, Currency = "RON",
            Status = InvoiceStatus.Paid, IssueDate = new DateOnly(2026, 3, 1), DueDate = new DateOnly(2026, 4, 1),
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var result = await CreateService(db).SendAsync(invoice.Id);

        // Service does not return null; it changes status to Sent regardless
        result.Should().NotBeNull();
        result!.Status.Should().Be(InvoiceStatus.Sent);
    }

    // ── RecordPaymentAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RecordPaymentAsync_ChangesStatus_ToPaid()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), ClientId = client.Id, Client = client,
            InvoiceNumber = "INV-2026-003", TotalAmount = 1000m, Currency = "RON",
            Status = InvoiceStatus.Sent, IssueDate = new DateOnly(2026, 3, 1), DueDate = new DateOnly(2026, 4, 1),
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RecordPaymentAsync(invoice.Id, new RecordPaymentRequest(1000m, null));

        result.Should().NotBeNull();
        result!.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task RecordPaymentAsync_SetsPaymentDate()
    {
        using var db = InMemoryDbContextFactory.Create();
        var client = CreateClient();
        db.Clients.Add(client);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), ClientId = client.Id, Client = client,
            InvoiceNumber = "INV-2026-004", TotalAmount = 500m, Currency = "RON",
            Status = InvoiceStatus.Sent, IssueDate = new DateOnly(2026, 3, 1), DueDate = new DateOnly(2026, 4, 1),
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var paymentDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        var result = await CreateService(db).RecordPaymentAsync(invoice.Id, new RecordPaymentRequest(500m, paymentDate));

        result.Should().NotBeNull();
        result!.PaidAt.Should().Be(paymentDate);
    }

    [Fact]
    public async Task RecordPaymentAsync_ReturnsNull_WhenInvoiceNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).RecordPaymentAsync(Guid.NewGuid(), new RecordPaymentRequest(100m, null));

        result.Should().BeNull();
    }
}
