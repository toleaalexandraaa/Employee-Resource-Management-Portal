using company_resource_manager.Server.Models.Common;

namespace company_resource_manager.Server.Models.Hr;

public class EmployeeDocument : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }

    public Employee Employee { get; set; } = null!;
}
