using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using company_resource_manager.Server.Models.Common;
using company_resource_manager.Server.Models.Crm;
using company_resource_manager.Server.Models.Hr;
using company_resource_manager.Server.Models.Identity;
using company_resource_manager.Server.Models.Proj;
using company_resource_manager.Server.Models.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Identity / Shared
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // HR
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<EmploymentContract> EmploymentContracts => Set<EmploymentContract>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();

    // Projects
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectTask> Tasks => Set<ProjectTask>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<KanbanBoard> KanbanBoards => Set<KanbanBoard>();
    public DbSet<KanbanColumn> KanbanColumns => Set<KanbanColumn>();

    // CRM
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientContact> ClientContacts => Set<ClientContact>();
    public DbSet<CrmContract> CrmContracts => Set<CrmContract>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Lead> Leads => Set<Lead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> implementations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = currentUserId;
                    var addedValues = entry.Properties
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                    auditEntries.Add(CreateAuditLog(entry, "Created", null, addedValues, currentUserId, now));
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    var oldValues = entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                    var newValues = entry.Properties
                        .Where(p => p.IsModified)
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                    auditEntries.Add(CreateAuditLog(entry, "Updated", oldValues, newValues, currentUserId, now));
                    break;

                case EntityState.Deleted:
                    // Soft delete: convert DELETE to UPDATE
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    auditEntries.Add(CreateAuditLog(entry, "Deleted", null, null, currentUserId, now));
                    break;
            }
        }

        AuditLogs.AddRange(auditEntries);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static AuditLog CreateAuditLog(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<AuditableEntity> entry,
        string action,
        object? oldValues,
        object? newValues,
        Guid changedBy,
        DateTime changedAt)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entry.Entity.GetType().Name,
            EntityId = entry.Entity.Id.ToString(),
            Action = action,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
            ChangedBy = changedBy,
            ChangedAt = changedAt,
        };
    }
}
