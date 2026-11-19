namespace company_resource_manager.Server.DTOs.Proj;

public record KanbanBoardDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    IReadOnlyList<KanbanColumnDto> Columns);

public record KanbanColumnDto(
    Guid Id,
    string Name,
    int Order,
    int? WipLimit,
    IReadOnlyList<ProjectTaskDto> Tasks);

public record ReorderColumnsRequest(IReadOnlyList<ColumnOrderItem> Columns);

public record ColumnOrderItem(Guid ColumnId, int NewOrder);
