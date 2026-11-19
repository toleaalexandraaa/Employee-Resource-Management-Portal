using Microsoft.AspNetCore.Identity;

namespace company_resource_manager.Server.Models.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
}
