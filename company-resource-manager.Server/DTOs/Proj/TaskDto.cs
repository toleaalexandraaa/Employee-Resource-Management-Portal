using company_resource_manager.Server.Models.Proj;
using TaskStatus = company_resource_manager.Server.Models.Proj.TaskStatus;

namespace company_resource_manager.Server.DTOs.Proj;

public record ProjectTaskDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string Title,
    string? Description,
    Guid? AssignedToEmployeeId,
    string? AssignedToEmployeeName,
    TaskPriority Priority,
    TaskStatus Status,
    decimal EstimatedHours,
    DateOnly? DueDate,
    Guid? KanbanColumnId,
    string? KanbanColumnName,
    int SortOrder);

public record CreateTaskRequest(
    Guid ProjectId,
    string Title,
    string? Description,
    Guid? AssignedToEmployeeId,
    TaskPriority Priority,
    decimal EstimatedHours,
    DateOnly? DueDate,
    Guid? KanbanColumnId);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    Guid? AssignedToEmployeeId,
    TaskPriority Priority,
    TaskStatus Status,
    decimal EstimatedHours,
    DateOnly? DueDate);

public record MoveTaskRequest(Guid? TargetColumnId, int NewSortOrder);
