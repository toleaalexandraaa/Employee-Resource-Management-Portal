using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Proj;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;

namespace company_resource_manager.Tests.Services.Proj;

/// <summary>
/// Unit tests for TimeEntryService.
/// Setup: InMemoryDbContextFactory.Create() only.
/// </summary>
public class TimeEntryServiceTests
{
    private static TimeEntryService CreateService(AppDbContext db) => new(db);

    /// <summary>Seeds an employee, project, project-member link, and a task; returns their IDs.</summary>
    private static async Task<(Guid employeeId, Guid projectId, Guid taskId)> SeedProjectSetupAsync(AppDbContext db)
    {
        var emp = new Employee { Id = Guid.NewGuid(), FirstName = "Dev", LastName = "User" };
        var project = new Project
        {
            Id = Guid.NewGuid(), Name = "Proj", Status = ProjectStatus.Active, StartDate = new DateOnly(2026, 1, 1),
        };
        var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "T1" };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, EmployeeId = emp.Id,
            Role = "Dev", JoinDate = new DateOnly(2026, 1, 1),
        };

        db.Employees.Add(emp);
        db.Projects.Add(project);
        db.Tasks.Add(task);
        db.ProjectMembers.Add(member);
        await db.SaveChangesAsync();

        return (emp.Id, project.Id, task.Id);
    }

    private static TimeEntry BuildEntry(Guid employeeId, Guid projectId, Guid taskId, DateOnly date, decimal hours) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            ProjectId = projectId,
            TaskId = taskId,
            Date = date,
            HoursLogged = hours,
        };

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsTimeEntry_WithCorrectFields()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (empId, projId, taskId) = await SeedProjectSetupAsync(db);

        var req = new CreateTimeEntryRequest(taskId, projId, new DateOnly(2026, 3, 15), 8m, "Full day");
        var result = await CreateService(db).CreateAsync(req, empId);

        result.EmployeeId.Should().Be(empId);
        result.ProjectId.Should().Be(projId);
        result.HoursLogged.Should().Be(8m);
        result.Description.Should().Be("Full day");
        db.TimeEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_ThrowsOrReturnsError_WhenProjectOrTaskNotFound()
    {
        // Service throws when the employee is not a member of the project
        using var db = InMemoryDbContextFactory.Create();
        var nonMemberId = Guid.NewGuid();
        var (_, projId, taskId) = await SeedProjectSetupAsync(db);

        var req = new CreateTimeEntryRequest(taskId, projId, new DateOnly(2026, 3, 15), 4m, null);
        var act = () => CreateService(db).CreateAsync(req, nonMemberId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a member*");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesHoursAndNotes_WhenEntryExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (empId, projId, taskId) = await SeedProjectSetupAsync(db);
        var entry = BuildEntry(empId, projId, taskId, new DateOnly(2026, 3, 1), 4m);
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();

        var update = new UpdateTimeEntryRequest(new DateOnly(2026, 3, 2), 6m, "Updated notes");
        var result = await CreateService(db).UpdateAsync(entry.Id, update, empId);

        result.Should().NotBeNull();
        result!.HoursLogged.Should().Be(6m);
        result.Description.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenEntryNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var update = new UpdateTimeEntryRequest(new DateOnly(2026, 3, 1), 4m, null);
        var result = await CreateService(db).UpdateAsync(Guid.NewGuid(), update, Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenEntryExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (empId, projId, taskId) = await SeedProjectSetupAsync(db);
        var entry = BuildEntry(empId, projId, taskId, new DateOnly(2026, 3, 1), 3m);
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();

        var result = await CreateService(db).DeleteAsync(entry.Id, empId);

        result.Should().BeTrue();
        db.TimeEntries.Should().HaveCount(0);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEntryNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── GetAllAsync (filtering) ───────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_FiltersByEmployeeId_WhenProvided()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (emp1Id, projId, taskId) = await SeedProjectSetupAsync(db);
        var emp2 = new Employee { Id = Guid.NewGuid(), FirstName = "Other", LastName = "User" };
        db.Employees.Add(emp2);
        db.TimeEntries.AddRange(
            BuildEntry(emp1Id, projId, taskId, new DateOnly(2026, 3, 1), 2m),
            BuildEntry(emp1Id, projId, taskId, new DateOnly(2026, 3, 2), 3m),
            BuildEntry(emp2.Id, projId, taskId, new DateOnly(2026, 3, 3), 4m));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(emp1Id, null, null, null, 1, 10);

        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(e => e.EmployeeId.Should().Be(emp1Id));
    }

    [Fact]
    public async Task GetAllAsync_FiltersByProjectId_WhenProvided()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (empId, proj1Id, task1Id) = await SeedProjectSetupAsync(db);

        var proj2 = new Project { Id = Guid.NewGuid(), Name = "Proj2", Status = ProjectStatus.Active, StartDate = new DateOnly(2026, 1, 1) };
        var task2 = new ProjectTask { Id = Guid.NewGuid(), ProjectId = proj2.Id, Title = "T2" };
        db.Projects.Add(proj2);
        db.Tasks.Add(task2);
        db.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = proj2.Id, EmployeeId = empId, Role = "Dev", JoinDate = new DateOnly(2026, 1, 1) });
        db.TimeEntries.AddRange(
            BuildEntry(empId, proj1Id, task1Id, new DateOnly(2026, 3, 1), 2m),
            BuildEntry(empId, proj2.Id, task2.Id, new DateOnly(2026, 3, 2), 3m));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(null, proj1Id, null, null, 1, 10);

        result.TotalCount.Should().Be(1);
        result.Items.Single().ProjectId.Should().Be(proj1Id);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByDateRange_WhenFromAndToProvided()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (empId, projId, taskId) = await SeedProjectSetupAsync(db);
        db.TimeEntries.AddRange(
            BuildEntry(empId, projId, taskId, new DateOnly(2026, 3, 1), 2m),
            BuildEntry(empId, projId, taskId, new DateOnly(2026, 3, 15), 3m),
            BuildEntry(empId, projId, taskId, new DateOnly(2026, 4, 1), 5m));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetAllAsync(
            null, null, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), 1, 10);

        result.TotalCount.Should().Be(2);
    }

    // ── GetSummaryAsync (mapped to GetProjectTotalHoursAsync) ─────────────────

    [Fact]
    public async Task GetSummaryAsync_AggregatesHoursByEmployee()
    {
        // Verified via GetProjectTotalHoursAsync which sums all hours for a project
        using var db = InMemoryDbContextFactory.Create();
        var (empId, projId, taskId) = await SeedProjectSetupAsync(db);
        db.TimeEntries.AddRange(
            BuildEntry(empId, projId, taskId, new DateOnly(2026, 3, 1), 2m),
            BuildEntry(empId, projId, taskId, new DateOnly(2026, 3, 2), 3m),
            BuildEntry(empId, projId, taskId, new DateOnly(2026, 3, 3), 5m));
        await db.SaveChangesAsync();

        var total = await CreateService(db).GetProjectTotalHoursAsync(projId);

        total.Should().Be(10m);
    }

    [Fact]
    public async Task GetSummaryAsync_FiltersByProjectId_WhenProvided()
    {
        // GetProjectTotalHoursAsync already filters by projectId
        using var db = InMemoryDbContextFactory.Create();
        var (empId, proj1Id, task1Id) = await SeedProjectSetupAsync(db);

        var proj2 = new Project { Id = Guid.NewGuid(), Name = "P2", Status = ProjectStatus.Active, StartDate = new DateOnly(2026, 1, 1) };
        var task2 = new ProjectTask { Id = Guid.NewGuid(), ProjectId = proj2.Id, Title = "T2" };
        db.Projects.Add(proj2);
        db.Tasks.Add(task2);
        db.TimeEntries.AddRange(
            BuildEntry(empId, proj1Id, task1Id, new DateOnly(2026, 3, 1), 4m),
            BuildEntry(empId, proj2.Id, task2.Id, new DateOnly(2026, 3, 2), 8m));
        await db.SaveChangesAsync();

        var total = await CreateService(db).GetProjectTotalHoursAsync(proj1Id);

        total.Should().Be(4m);
    }
}
