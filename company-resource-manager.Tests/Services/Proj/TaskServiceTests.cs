using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Models.Shared;
using company_resource_manager.Server.Services.Interfaces;
using company_resource_manager.Server.Services.Proj;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskStatus = company_resource_manager.Server.Models.Proj.TaskStatus;

namespace company_resource_manager.Tests.Services.Proj;

public class TaskServiceTests
{
    private readonly Mock<INotificationService> _notifications;
    private readonly Mock<UserManager<ApplicationUser>> _userManager;

    public TaskServiceTests()
    {
        _notifications = new Mock<INotificationService>();
        _userManager = MockIdentityFactory.CreateUserManager();
    }

    private TaskService CreateService(AppDbContext db) =>
        new(db, _notifications.Object, _userManager.Object);

    private static Project CreateProject() =>
        new() { Id = Guid.NewGuid(), Name = "Test Project", Status = ProjectStatus.Active };

    private static Employee CreateEmployee(string first = "John", string last = "Doe") =>
        new() { Id = Guid.NewGuid(), FirstName = first, LastName = last };

    private static KanbanColumn CreateColumn(string name = "To Do", int order = 0) =>
        new() { Id = Guid.NewGuid(), Name = name, Order = order };

    private static ProjectTask CreateTask(Guid projectId, Guid? columnId = null, int sortOrder = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = "Test Task",
            Status = TaskStatus.Backlog,
            Priority = TaskPriority.Medium,
            SortOrder = sortOrder,
            KanbanColumnId = columnId,
        };

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SetsFirstSortOrderToZero_WhenColumnIsEmpty()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn();
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        await db.SaveChangesAsync();

        // No assignee — UserManager.Users will not be queried
        var request = new CreateTaskRequest(project.Id, "Task 1", null, null, TaskPriority.Medium, 0m, null, column.Id);
        var result = await CreateService(db).CreateAsync(request, Guid.NewGuid());

        result.SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_AppendsSortOrder_AfterExistingTasksInSameColumn()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn();
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        db.Tasks.Add(CreateTask(project.Id, column.Id, sortOrder: 4));
        await db.SaveChangesAsync();

        var request = new CreateTaskRequest(project.Id, "New Task", null, null, TaskPriority.Low, 0m, null, column.Id);
        var result = await CreateService(db).CreateAsync(request, Guid.NewGuid());

        result.SortOrder.Should().Be(5);
    }

    [Fact]
    public async Task CreateAsync_SortOrderIsIndependentPerColumn()
    {
        // Two columns each with tasks — sort order of one should not affect the other
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var colA = CreateColumn("To Do", 0);
        var colB = CreateColumn("In Progress", 1);
        db.Projects.Add(project);
        db.KanbanColumns.AddRange(colA, colB);
        db.Tasks.Add(CreateTask(project.Id, colA.Id, sortOrder: 10));
        await db.SaveChangesAsync();

        var request = new CreateTaskRequest(project.Id, "Task in B", null, null, TaskPriority.Low, 0m, null, colB.Id);
        var result = await CreateService(db).CreateAsync(request, Guid.NewGuid());

        result.SortOrder.Should().Be(0);  // Column B had no tasks → starts at 0
    }

    [Fact]
    public async Task CreateAsync_NotifiesAssignee_WhenAssigneeIsProvided()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn();
        var employee = CreateEmployee();
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var assigneeUser = new ApplicationUser { Id = Guid.NewGuid(), EmployeeId = employee.Id };
        _userManager.Setup(m => m.Users)
            .Returns(new TestAsyncEnumerable<ApplicationUser>([assigneeUser]));

        var request = new CreateTaskRequest(project.Id, "Assigned Task", null, employee.Id, TaskPriority.High, 0m, null, column.Id);
        await CreateService(db).CreateAsync(request, Guid.NewGuid());

        _notifications.Verify(n => n.CreateAsync(
            assigneeUser.Id,
            NotificationType.TaskAssigned,
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DoesNotNotify_WhenNoAssignee()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn();
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        await db.SaveChangesAsync();

        var request = new CreateTaskRequest(project.Id, "Unassigned Task", null, null, TaskPriority.Low, 0m, null, column.Id);
        await CreateService(db).CreateAsync(request, Guid.NewGuid());

        _notifications.Verify(n => n.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PersistsTaskWithCorrectFields()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn();
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        await db.SaveChangesAsync();

        var createdBy = Guid.NewGuid();
        var request = new CreateTaskRequest(project.Id, "My Task", "Some description", null, TaskPriority.Critical, 8m, new DateOnly(2026, 4, 1), column.Id);
        var result = await CreateService(db).CreateAsync(request, createdBy);

        result.Title.Should().Be("My Task");
        result.Priority.Should().Be(TaskPriority.Critical);
        result.Status.Should().Be(TaskStatus.Backlog);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenTaskNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).UpdateAsync(
            Guid.NewGuid(),
            new UpdateTaskRequest("Title", null, null, TaskPriority.Low, TaskStatus.Backlog, 0m, null));

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTaskFields()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn();
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        var task = CreateTask(project.Id, column.Id);
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var result = await CreateService(db).UpdateAsync(
            task.Id,
            new UpdateTaskRequest("Updated Title", "New desc", null, TaskPriority.High, TaskStatus.InProgress, 4m, null));

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Status.Should().Be(TaskStatus.InProgress);
        result.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task UpdateAsync_NotifiesNewAssignee_WhenAssigneeChanges()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn();
        var oldEmployee = CreateEmployee("Old", "Emp");
        var newEmployee = CreateEmployee("New", "Emp");
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        db.Employees.AddRange(oldEmployee, newEmployee);
        var task = CreateTask(project.Id, column.Id);
        task.AssignedToEmployeeId = oldEmployee.Id;
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var newUser = new ApplicationUser { Id = Guid.NewGuid(), EmployeeId = newEmployee.Id };
        _userManager.Setup(m => m.Users)
            .Returns(new TestAsyncEnumerable<ApplicationUser>([newUser]));

        await CreateService(db).UpdateAsync(
            task.Id,
            new UpdateTaskRequest("Task", null, newEmployee.Id, TaskPriority.Medium, TaskStatus.InProgress, 0m, null));

        _notifications.Verify(n => n.CreateAsync(
            newUser.Id, NotificationType.TaskAssigned, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotNotify_WhenAssigneeIsUnchanged()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn();
        var employee = CreateEmployee();
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        db.Employees.Add(employee);
        var task = CreateTask(project.Id, column.Id);
        task.AssignedToEmployeeId = employee.Id;
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        await CreateService(db).UpdateAsync(
            task.Id,
            new UpdateTaskRequest("Same Task", null, employee.Id, TaskPriority.High, TaskStatus.InProgress, 0m, null));

        _notifications.Verify(n => n.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotNotify_WhenAssigneeIsCleared()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn();
        var employee = CreateEmployee();
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        db.Employees.Add(employee);
        var task = CreateTask(project.Id, column.Id);
        task.AssignedToEmployeeId = employee.Id;
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        _userManager.Setup(m => m.Users).Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        await CreateService(db).UpdateAsync(
            task.Id,
            new UpdateTaskRequest("Task", null, null, TaskPriority.Medium, TaskStatus.Backlog, 0m, null));

        _notifications.Verify(n => n.CreateAsync(
            It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── MoveAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveAsync_SetsStatusToDone_WhenTargetColumnNameIsDone()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var doneColumn = CreateColumn("Done", 2);
        db.Projects.Add(project);
        db.KanbanColumns.Add(doneColumn);
        var task = CreateTask(project.Id);
        task.Status = TaskStatus.InProgress;
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var result = await CreateService(db).MoveAsync(task.Id, new MoveTaskRequest(doneColumn.Id, 0));

        result.Should().NotBeNull();
        result!.Status.Should().Be(TaskStatus.Done);
    }

    [Fact]
    public async Task MoveAsync_DoesNotChangeStatus_WhenTargetColumnIsNotDone()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var inProgressColumn = CreateColumn("In Progress", 1);
        db.Projects.Add(project);
        db.KanbanColumns.Add(inProgressColumn);
        var task = CreateTask(project.Id);
        task.Status = TaskStatus.Backlog;
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var result = await CreateService(db).MoveAsync(task.Id, new MoveTaskRequest(inProgressColumn.Id, 1));

        result!.Status.Should().Be(TaskStatus.Backlog);
    }

    [Fact]
    public async Task MoveAsync_UpdatesSortOrder()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn("To Do");
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        var task = CreateTask(project.Id, column.Id, sortOrder: 0);
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        await CreateService(db).MoveAsync(task.Id, new MoveTaskRequest(column.Id, 3));

        db.Tasks.Find(task.Id)!.SortOrder.Should().Be(3);
    }

    [Fact]
    public async Task MoveAsync_ReturnsNull_WhenTaskNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).MoveAsync(Guid.NewGuid(), new MoveTaskRequest(null, 0));

        result.Should().BeNull();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemovesTask()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        db.Projects.Add(project);
        var task = CreateTask(project.Id);
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var result = await CreateService(db).DeleteAsync(task.Id);

        result.Should().BeTrue();
        // Soft-delete converts DELETE to UPDATE with IsDeleted = true
        db.Tasks.IgnoreQueryFilters().Single().IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenTaskNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenTaskNotFound()
    {
        using var db = InMemoryDbContextFactory.Create();

        var result = await CreateService(db).GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_IncludesAssigneeName_WhenAssigned()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = CreateProject();
        var column = CreateColumn();
        var employee = CreateEmployee("Alice", "Wonder");
        db.Projects.Add(project);
        db.KanbanColumns.Add(column);
        db.Employees.Add(employee);
        var task = CreateTask(project.Id, column.Id);
        task.AssignedToEmployeeId = employee.Id;
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetByIdAsync(task.Id);

        result.Should().NotBeNull();
        result!.AssignedToEmployeeName.Should().Be("Alice Wonder");
    }
}
