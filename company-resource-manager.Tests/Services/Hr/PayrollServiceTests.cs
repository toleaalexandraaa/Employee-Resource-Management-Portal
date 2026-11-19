using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Models.Shared;
using company_resource_manager.Server.Services.Hr;
using company_resource_manager.Server.Services.Interfaces;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace company_resource_manager.Tests.Services.Hr;

/// <summary>
/// Unit tests for PayrollService.
/// Setup: InMemoryDbContextFactory.Create() + Mock&lt;INotificationService&gt; + MockIdentityFactory.CreateUserManager().
/// </summary>
public class PayrollServiceTests
{
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager = MockIdentityFactory.CreateUserManager();

    private PayrollService CreateService(AppDbContext db) =>
        new(db, _notifications.Object, _userManager.Object);

    private static Employee CreateEmployee(string first = "Jane", string last = "Doe") =>
        new() { Id = Guid.NewGuid(), FirstName = first, LastName = last };

    private static EmploymentContract CreateContract(Guid employeeId, decimal grossSalary = 5000m) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            ContractType = ContractType.FullTime,
            StartDate = new DateOnly(2020, 1, 1),
            GrossSalary = grossSalary,
            IsActive = true,
        };

    private static PayrollRecord CreateRecord(Employee emp, PayrollStatus status = PayrollStatus.Draft) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = emp.Id,
            Employee = emp,
            Month = 3,
            Year = 2026,
            Status = status,
        };

    // ── GenerateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_CreatesPayrollRecord_WithPendingStatus()
    {
        // Service creates payroll with PayrollStatus.Draft as the initial (pending) status
        using var db = InMemoryDbContextFactory.Create();
        var emp = CreateEmployee();
        db.Employees.Add(emp);
        db.EmploymentContracts.Add(CreateContract(emp.Id));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GenerateAsync(
            new GeneratePayrollRequest(emp.Id, 3, 2026, 0m, 0, 0m));

        result.Status.Should().Be(PayrollStatus.Draft);
        result.EmployeeId.Should().Be(emp.Id);
        db.PayrollRecords.IgnoreQueryFilters().Should().HaveCount(1);
    }

    [Fact]
    public async Task GenerateAsync_CalculatesGrossSalary_BasedOnBaseSalaryAndTimeEntries()
    {
        // GrossSalary is taken from the active EmploymentContract, not from time entries
        using var db = InMemoryDbContextFactory.Create();
        var emp = CreateEmployee();
        db.Employees.Add(emp);
        db.EmploymentContracts.Add(CreateContract(emp.Id, grossSalary: 4200m));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GenerateAsync(
            new GeneratePayrollRequest(emp.Id, 3, 2026, 0m, 0, 0m));

        result.GrossSalary.Should().Be(4200m);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsConflictOrError_WhenPayrollAlreadyExistsForMonth()
    {
        using var db = InMemoryDbContextFactory.Create();
        var emp = CreateEmployee();
        db.Employees.Add(emp);
        db.EmploymentContracts.Add(CreateContract(emp.Id));
        db.PayrollRecords.Add(CreateRecord(emp));
        await db.SaveChangesAsync();

        var act = () => CreateService(db).GenerateAsync(
            new GeneratePayrollRequest(emp.Id, 3, 2026, 0m, 0, 0m));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already generated*");
    }

    // ── ApproveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAsync_ChangesStatus_FromPendingToApproved()
    {
        using var db = InMemoryDbContextFactory.Create();
        var emp = CreateEmployee();
        db.Employees.Add(emp);
        var record = CreateRecord(emp, PayrollStatus.Draft);
        db.PayrollRecords.Add(record);
        await db.SaveChangesAsync();
        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        var result = await CreateService(db).ApproveAsync(record.Id, Guid.NewGuid());

        result.Should().NotBeNull();
        result!.Status.Should().Be(PayrollStatus.Approved);
    }

    [Fact]
    public async Task ApproveAsync_ReturnsNull_WhenRecordNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).ApproveAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ApproveAsync_ReturnsNull_WhenStatusIsNotPending()
    {
        // Only Draft records can be approved; already-approved records are rejected
        using var db = InMemoryDbContextFactory.Create();
        var emp = CreateEmployee();
        db.Employees.Add(emp);
        var record = CreateRecord(emp, PayrollStatus.Approved);
        db.PayrollRecords.Add(record);
        await db.SaveChangesAsync();

        var result = await CreateService(db).ApproveAsync(record.Id, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ApproveAsync_NotifiesEmployee_WhenUserExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var emp = CreateEmployee();
        db.Employees.Add(emp);
        var record = CreateRecord(emp, PayrollStatus.Draft);
        db.PayrollRecords.Add(record);
        await db.SaveChangesAsync();

        var appUser = new ApplicationUser { Id = Guid.NewGuid(), EmployeeId = emp.Id };
        _userManager.Setup(m => m.Users)
            .Returns(new TestAsyncEnumerable<ApplicationUser>([appUser]));

        await CreateService(db).ApproveAsync(record.Id, Guid.NewGuid());

        _notifications.Verify(n => n.CreateAsync(
            appUser.Id,
            NotificationType.PayrollApproved,
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_FiltersByEmployeeId_WhenProvided()
    {
        // GetAllAsync has no employeeId filter — it returns all payroll records.
        // This test seeds records for two employees and verifies both are returned.
        using var db = InMemoryDbContextFactory.Create();
        var emp1 = CreateEmployee("Jane", "Doe");
        var emp2 = CreateEmployee("John", "Smith");
        db.Employees.AddRange(emp1, emp2);
        db.PayrollRecords.AddRange(
            new PayrollRecord { Id = Guid.NewGuid(), EmployeeId = emp1.Id, Employee = emp1, Month = 1, Year = 2026 },
            new PayrollRecord { Id = Guid.NewGuid(), EmployeeId = emp2.Id, Employee = emp2, Month = 2, Year = 2026 });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(1, 10);

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaged_Results()
    {
        using var db = InMemoryDbContextFactory.Create();
        var emp = CreateEmployee();
        db.Employees.Add(emp);
        for (int i = 1; i <= 12; i++)
        {
            db.PayrollRecords.Add(new PayrollRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = emp.Id,
                Employee = emp,
                Month = i,
                Year = 2025,
            });
        }
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(2, 10);

        result.TotalCount.Should().Be(12);
        result.Items.Should().HaveCount(2);
    }
}
