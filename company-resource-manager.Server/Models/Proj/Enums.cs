namespace company_resource_manager.Server.Models.Proj;

public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Completed,
    Cancelled,
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical,
}

public enum TaskStatus
{
    Backlog,
    InProgress,
    InReview,
    Done,
}
