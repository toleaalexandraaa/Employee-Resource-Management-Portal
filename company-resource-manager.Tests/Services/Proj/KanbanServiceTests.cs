using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Services.Interfaces;
using company_resource_manager.Server.Services.Proj;
using company_resource_manager.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace company_resource_manager.Tests.Services.Proj;

/// <summary>
/// Unit tests for KanbanService.
/// Setup: InMemoryDbContextFactory.Create() + Mock&lt;ITaskService&gt;.
/// </summary>
public class KanbanServiceTests
{
    private readonly Mock<ITaskService> _taskService = new();

    private KanbanService CreateService(AppDbContext db) => new(db, _taskService.Object);

    /// <summary>Seeds a project + board + columns and returns their IDs.</summary>
    private static async Task<(Guid projectId, Guid boardId, List<KanbanColumn> columns)> SeedBoardAsync(
        AppDbContext db, int[] columnOrders)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            Status = ProjectStatus.Planning,
            StartDate = new DateOnly(2026, 1, 1),
        };
        db.Projects.Add(project);

        var board = new KanbanBoard { Id = Guid.NewGuid(), ProjectId = project.Id };
        db.KanbanBoards.Add(board);

        var columns = columnOrders.Select(o => new KanbanColumn
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            Name = $"Col-{o}",
            Order = o,
        }).ToList();
        db.KanbanColumns.AddRange(columns);

        await db.SaveChangesAsync();
        return (project.Id, board.Id, columns);
    }

    // ── GetBoardAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBoardAsync_ReturnsBoardWithColumnsOrderedByColumnIndex()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (projectId, _, _) = await SeedBoardAsync(db, [2, 0, 1]);

        var result = await CreateService(db).GetBoardAsync(projectId);

        result.Should().NotBeNull();
        var orders = result!.Columns.Select(c => c.Order).ToList();
        orders.Should().BeInAscendingOrder();
        orders.Should().BeEquivalentTo([0, 1, 2], o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task GetBoardAsync_IncludesTasks_InEachColumn()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (projectId, _, columns) = await SeedBoardAsync(db, [0, 1]);

        var col0 = columns.Single(c => c.Order == 0);
        var col1 = columns.Single(c => c.Order == 1);

        db.Tasks.AddRange(
            new ProjectTask { Id = Guid.NewGuid(), ProjectId = projectId, KanbanColumnId = col0.Id, Title = "TaskA" },
            new ProjectTask { Id = Guid.NewGuid(), ProjectId = projectId, KanbanColumnId = col0.Id, Title = "TaskB" },
            new ProjectTask { Id = Guid.NewGuid(), ProjectId = projectId, KanbanColumnId = col1.Id, Title = "TaskC" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetBoardAsync(projectId);

        result.Should().NotBeNull();
        var colDtos = result!.Columns.OrderBy(c => c.Order).ToList();
        colDtos[0].Tasks.Should().HaveCount(2);
        colDtos[1].Tasks.Should().HaveCount(1);
        colDtos[1].Tasks.Single().Title.Should().Be("TaskC");
    }

    [Fact]
    public async Task GetBoardAsync_ReturnsNull_WhenProjectHasNoBoard()
    {
        using var db = InMemoryDbContextFactory.Create();
        var project = new Project
        {
            Id = Guid.NewGuid(), Name = "P", Status = ProjectStatus.Planning, StartDate = new DateOnly(2026, 1, 1),
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetBoardAsync(project.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBoardAsync_ExcludesSoftDeletedTasks()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (projectId, _, columns) = await SeedBoardAsync(db, [0]);

        var col = columns[0];
        var task = new ProjectTask
        {
            Id = Guid.NewGuid(), ProjectId = projectId, KanbanColumnId = col.Id, Title = "ToDelete",
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        // Soft-delete the task
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetBoardAsync(projectId);

        result.Should().NotBeNull();
        result!.Columns.SelectMany(c => c.Tasks).Should().BeEmpty();
    }

    // ── ReorderColumnsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ReorderColumnsAsync_UpdatesColumnIndices_ToMatchRequest()
    {
        using var db = InMemoryDbContextFactory.Create();
        var (projectId, _, columns) = await SeedBoardAsync(db, [0, 1, 2]);

        // Reverse the order
        var reorderRequest = new ReorderColumnsRequest(
            columns.Select((c, i) => new ColumnOrderItem(c.Id, 2 - i)).ToList());

        await CreateService(db).ReorderColumnsAsync(projectId, reorderRequest);

        var updated = db.KanbanColumns.IgnoreQueryFilters().ToList();
        foreach (var item in reorderRequest.Columns)
        {
            updated.Single(c => c.Id == item.ColumnId).Order.Should().Be(item.NewOrder);
        }
    }

    [Fact]
    public async Task ReorderColumnsAsync_ReturnsNull_WhenBoardNotFound()
    {
        // ReorderColumnsAsync is void — it silently does nothing when board is not found.
        using var db = InMemoryDbContextFactory.Create();
        var request = new ReorderColumnsRequest([new ColumnOrderItem(Guid.NewGuid(), 0)]);

        var act = () => CreateService(db).ReorderColumnsAsync(Guid.NewGuid(), request);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReorderColumnsAsync_IgnoresColumns_ThatDontBelongToBoard()
    {
        using var db = InMemoryDbContextFactory.Create();
        // Board 1
        var (projectId, _, cols1) = await SeedBoardAsync(db, [0, 1]);
        // Board 2 (different project/board)
        var (_, _, cols2) = await SeedBoardAsync(db, [0, 1]);

        var originalOrdersBoard2 = cols2.ToDictionary(c => c.Id, c => c.Order);

        // Try to reorder board1's project using board2's column IDs
        var request = new ReorderColumnsRequest(
            cols2.Select((c, i) => new ColumnOrderItem(c.Id, 99 - i)).ToList());

        await CreateService(db).ReorderColumnsAsync(projectId, request);

        // Board 2's columns should be unchanged
        var afterUpdate = db.KanbanColumns.IgnoreQueryFilters().ToList();
        foreach (var (id, originalOrder) in originalOrdersBoard2)
        {
            afterUpdate.Single(c => c.Id == id).Order.Should().Be(originalOrder);
        }
    }
}
