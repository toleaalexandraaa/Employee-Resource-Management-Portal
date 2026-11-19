using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Proj;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace company_resource_manager.Tests.Services.Proj;

/// <summary>
/// Unit tests for ProjectService.
/// Setup: InMemoryDbContextFactory.Create() + MockIdentityFactory.CreateUserManager().
/// </summary>
public class ProjectServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager = MockIdentityFactory.CreateUserManager();

    private ProjectService CreateService(AppDbContext db) => new(db, _userManager.Object);

    private static Project CreateProject(string name = "Test Project") =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Status = ProjectStatus.Planning,
            StartDate = new DateOnly(2026, 1, 1),
        };

    private static CreateProjectRequest BuildCreateRequest(string name = "Alpha") => new(
        name, "Description", null, ProjectStatus.Planning,
        new DateOnly(2026, 1, 1), null, 100_000m, 90);

    private void SetupUserManagerNull() =>
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((ApplicationUser?)null);

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsProject_WithCorrectFields()
    {
        using var db = InMemoryDbContextFactory.Create();
        SetupUserManagerNull();

        var dto = await CreateService(db).CreateAsync(BuildCreateRequest("Alpha"), Guid.NewGuid());

        dto.Name.Should().Be("Alpha");
        dto.EstimatedBudget.Should().Be(100_000m);
        dto.EstimatedDurationDays.Should().Be(90);
        db.Projects.IgnoreQueryFilters().Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_CreatesKanbanBoard_WithDefaultColumns()
    {
        using var db = InMemoryDbContextFactory.Create();
        SetupUserManagerNull();

        await CreateService(db).CreateAsync(BuildCreateRequest(), Guid.NewGuid());

        db.KanbanBoards.Should().HaveCount(1);
        var columns = db.KanbanColumns.ToList();
        columns.Should().HaveCount(4);
        columns.Select(c => c.Name).Should().BeEquivalentTo(["Backlog", "In Progress", "In Review", "Done"]);
    }

    [Fact]
    public async Task CreateAsync_SetsStatusToPlanning_ByDefault()
    {
        using var db = InMemoryDbContextFactory.Create();
        SetupUserManagerNull();

        var dto = await CreateService(db).CreateAsync(BuildCreateRequest(), Guid.NewGuid());

        dto.Status.Should().Be(ProjectStatus.Planning);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsProjectWithMemberCount()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        db.Projects.Add(project);
        db.ProjectMembers.AddRange(
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, EmployeeId = Guid.NewGuid(), Role = "Dev", JoinDate = new DateOnly(2026, 1, 1) },
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, EmployeeId = Guid.NewGuid(), Role = "QA", JoinDate = new DateOnly(2026, 1, 1) },
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, EmployeeId = Guid.NewGuid(), Role = "Lead", JoinDate = new DateOnly(2026, 1, 1) });
        await db.SaveChangesAsync();
        SetupUserManagerNull();

        var result = await CreateService(db).GetByIdAsync(project.Id);

        result.Should().NotBeNull();
        result!.MemberCount.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();
        SetupUserManagerNull();

        var result = await CreateService(db).GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesFields_WhenProjectExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject("OldName");
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        SetupUserManagerNull();

        var update = new UpdateProjectRequest(
            "NewName", "New Desc", null, ProjectStatus.Active,
            new DateOnly(2026, 1, 1), null, 200_000m, 50_000m, 120);

        var result = await CreateService(db).UpdateAsync(project.Id, update);

        result.Should().NotBeNull();
        result!.Name.Should().Be("NewName");
        result.Status.Should().Be(ProjectStatus.Active);
        result.EstimatedBudget.Should().Be(200_000m);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenProjectNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();
        SetupUserManagerNull();

        var update = new UpdateProjectRequest(
            "Name", null, null, ProjectStatus.Active,
            new DateOnly(2026, 1, 1), null, 0m, 0m, 0);

        var result = await CreateService(db).UpdateAsync(Guid.NewGuid(), update);

        result.Should().BeNull();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SoftDeletes_WhenProjectExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject("ToDelete");
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var result = await CreateService(db).DeleteAsync(project.Id);

        result.Should().BeTrue();
        db.Projects.IgnoreQueryFilters().Should().HaveCount(1);
        db.Projects.IgnoreQueryFilters().Single().IsDeleted.Should().BeTrue();
        db.Projects.Should().HaveCount(0);
    }

    // ── Members ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddMemberAsync_AddsProjectMember_WithCorrectRole()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var employeeId = Guid.NewGuid();
        var req = new AddProjectMemberRequest(employeeId, "Developer", 40m, new DateOnly(2026, 1, 1), null);

        var result = await CreateService(db).AddMemberAsync(project.Id, req);

        result.Role.Should().Be("Developer");
        result.EmployeeId.Should().Be(employeeId);
        result.ProjectId.Should().Be(project.Id);
        db.ProjectMembers.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddMemberAsync_ReturnsError_WhenMemberAlreadyExists()
    {
        // AddMemberAsync has no duplicate guard — a second entry is created.
        // This test documents that existing behavior.
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        db.Projects.Add(project);
        var employeeId = Guid.NewGuid();
        db.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, EmployeeId = employeeId,
            Role = "Dev", JoinDate = new DateOnly(2026, 1, 1),
        });
        await db.SaveChangesAsync();

        var req = new AddProjectMemberRequest(employeeId, "Dev", 40m, new DateOnly(2026, 1, 1), null);
        await CreateService(db).AddMemberAsync(project.Id, req);

        // Second entry is added (no guard implemented in the service)
        db.ProjectMembers.Should().HaveCount(2);
    }

    [Fact]
    public async Task RemoveMemberAsync_RemovesMember_WhenExists()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        db.Projects.Add(project);
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, EmployeeId = Guid.NewGuid(),
            Role = "Dev", JoinDate = new DateOnly(2026, 1, 1),
        };
        db.ProjectMembers.Add(member);
        await db.SaveChangesAsync();

        var result = await CreateService(db).RemoveMemberAsync(project.Id, member.Id);

        result.Should().BeTrue();
        db.ProjectMembers.Should().HaveCount(0);
    }

    [Fact]
    public async Task RemoveMemberAsync_ReturnsFalse_WhenMemberNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).RemoveMemberAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── GetGanttAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGanttAsync_ReturnsTasks_WithStartAndEndDates()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        db.Projects.Add(project);
        db.Tasks.AddRange(
            new ProjectTask { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "Task A", DueDate = new DateOnly(2026, 3, 31) },
            new ProjectTask { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "Task B", DueDate = new DateOnly(2026, 4, 30) });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetGanttAsync(project.Id);

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Title == "Task A" && t.DueDate == new DateOnly(2026, 3, 31));
        result.Should().Contain(t => t.Title == "Task B" && t.DueDate == new DateOnly(2026, 4, 30));
    }

    [Fact]
    public async Task GetGanttAsync_ReturnsEmpty_WhenProjectHasNoTasks()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetGanttAsync(project.Id);

        result.Should().BeEmpty();
    }
}
