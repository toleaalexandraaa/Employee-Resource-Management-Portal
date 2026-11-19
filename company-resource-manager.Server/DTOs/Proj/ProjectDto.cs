using company_resource_manager.Server.Models.Proj;

namespace company_resource_manager.Server.DTOs.Proj;

public record ProjectListItemDto(
    Guid Id,
    string Name,
    string? ClientName,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal EstimatedBudget,
    Guid ProjectManagerId,
    string ProjectManagerName,
    int MemberCount,
    int TaskCount);

public record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ClientId,
    string? ClientName,
    Guid ProjectManagerId,
    string ProjectManagerName,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal EstimatedBudget,
    decimal ActualBudget,
    int EstimatedDurationDays,
    int MemberCount,
    int TaskCount);

public record CreateProjectRequest(
    string Name,
    string? Description,
    Guid? ClientId,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal EstimatedBudget,
    int EstimatedDurationDays);

public record UpdateProjectRequest(
    string Name,
    string? Description,
    Guid? ClientId,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal EstimatedBudget,
    decimal ActualBudget,
    int EstimatedDurationDays);
