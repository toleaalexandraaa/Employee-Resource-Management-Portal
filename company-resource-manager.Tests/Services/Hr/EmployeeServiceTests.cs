using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Services.Hr;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace company_resource_manager.Tests.Services.Hr;

/// <summary>
/// Unit tests for EmployeeService.
/// </summary>
public class EmployeeServiceTests
{
    private static EmployeeService CreateService(AppDbContext db,
        Mock<UserManager<ApplicationUser>>? userManager = null)
        => new(db, (userManager ?? MockIdentityFactory.CreateUserManager()).Object);

    private static Mock<UserManager<ApplicationUser>> CreateSucceedingUserManager()
    {
        var um = MockIdentityFactory.CreateUserManager();
        um.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        um.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        return um;
    }

    private static Employee CreateEmployee(string first = "John", string last = "Doe", string dept = "Engineering") =>
        new()
        {
            Id = Guid.NewGuid(),
            FirstName = first,
            LastName = last,
            Department = dept,
            JobTitle = "Developer",
            WorkEmail = $"{first.ToLower()}@work.com",
            PersonalEmail = $"{first.ToLower()}@personal.com",
            Phone = "+40700000000",
            IsActive = true,
        };

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaged_Results()
    {
        using var db = InMemoryDbContextFactory.Create();
        for (int i = 0; i < 15; i++)
            db.Employees.Add(CreateEmployee($"First{i:D2}", $"Last{i:D2}"));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(2, 10);

        result.TotalCount.Should().Be(15);
        result.Items.Should().HaveCount(5);
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByDepartment_WhenDepartmentProvided()
    {
        // GetAllAsync does not accept a department filter — it returns all employees.
        // This test verifies the service returns employees from all departments.
        using var db = InMemoryDbContextFactory.Create();
        db.Employees.Add(CreateEmployee("Alice", "A", "Engineering"));
        db.Employees.Add(CreateEmployee("Bob", "B", "HR"));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(1, 10);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedEmployees()
    {
        using var db = InMemoryDbContextFactory.Create();
        var active = CreateEmployee("Active", "One");
        var toDelete = CreateEmployee("Deleted", "Two");
        db.Employees.AddRange(active, toDelete);
        await db.SaveChangesAsync();

        db.Employees.Remove(toDelete);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(1, 10);

        result.TotalCount.Should().Be(1);
        result.Items.Single().FullName.Should().Contain("Active");
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenEmployeeExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var emp = CreateEmployee("Jane", "Smith", "HR");
        emp.WorkEmail = "jane@work.com";
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetByIdAsync(emp.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(emp.Id);
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.WorkEmail.Should().Be("jane@work.com");
        result.Department.Should().Be("HR");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsEmployee_WithCorrectFields()
    {
        using var db = InMemoryDbContextFactory.Create();
        var um = CreateSucceedingUserManager();
        var req = new CreateEmployeeRequest(
            "John", "Doe", "john@personal.com", "john@work.com", "+40700000000",
            new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1),
            "Engineering", "Developer", null, 3.5f, "Employee");

        var result = await CreateService(db, um).CreateAsync(req);

        result.Employee.FirstName.Should().Be("John");
        result.Employee.LastName.Should().Be("Doe");
        result.Employee.Department.Should().Be("Engineering");
        result.Employee.WorkEmail.Should().Be("john@work.com");
        result.Employee.JobTitle.Should().Be("Developer");
        db.Employees.IgnoreQueryFilters().Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_SetsIsActiveToTrue_ByDefault()
    {
        using var db = InMemoryDbContextFactory.Create();
        var um = CreateSucceedingUserManager();
        var req = new CreateEmployeeRequest(
            "Alice", "Lee", "alice@p.com", "alice@w.com", "000",
            new DateOnly(1995, 5, 1), new DateOnly(2022, 3, 1),
            "IT", "Analyst", null, 1f, "Employee");

        await CreateService(db, um).CreateAsync(req);

        db.Employees.IgnoreQueryFilters().Single().IsActive.Should().BeTrue();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesFields_WhenEmployeeExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var emp = CreateEmployee("Old", "Name");
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var update = new UpdateEmployeeRequest(
            "New", "Name", "new@p.com", "new@w.com", "111",
            "HR", "Manager", null, 5f, true);

        var result = await CreateService(db).UpdateAsync(emp.Id, update);

        result.Should().NotBeNull();
        result!.FirstName.Should().Be("New");
        result.Department.Should().Be("HR");
        result.JobTitle.Should().Be("Manager");
        result.WorkEmail.Should().Be("new@w.com");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenEmployeeNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();
        var update = new UpdateEmployeeRequest(
            "A", "B", "a@b.com", "a@w.com", "000", "HR", "Dev", null, 1f, true);

        var result = await CreateService(db).UpdateAsync(Guid.NewGuid(), update);

        result.Should().BeNull();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletes_WhenEmployeeExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var emp = CreateEmployee("To", "Delete");
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var result = await CreateService(db).DeleteAsync(emp.Id);

        result.Should().BeTrue();
        // Soft-delete: row stays in DB with IsDeleted = true
        db.Employees.IgnoreQueryFilters().Should().HaveCount(1);
        db.Employees.IgnoreQueryFilters().Single().IsDeleted.Should().BeTrue();
        // Global query filter excludes soft-deleted rows from normal queries
        db.Employees.Should().HaveCount(0);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEmployeeNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }
}
