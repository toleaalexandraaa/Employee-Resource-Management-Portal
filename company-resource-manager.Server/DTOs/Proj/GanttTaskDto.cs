using company_resource_manager.Server.Models.Proj;
using TaskStatus = company_resource_manager.Server.Models.Proj.TaskStatus;

namespace company_resource_manager.Server.DTOs.Proj;

public record GanttTaskDto(
    Guid Id,
    string Title,
    DateOnly? StartDate,
    DateOnly? DueDate,
    Guid? AssignedToEmployeeId,
    string? AssignedToEmployeeName,
    TaskStatus Status,
    TaskPriority Priority,
    decimal EstimatedHours);
