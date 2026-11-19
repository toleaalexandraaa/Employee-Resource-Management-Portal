using company_resource_manager.Server.Models.Proj;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Proj;

public class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("Tasks", "proj");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(256);
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.EstimatedHours).HasPrecision(6, 2);

        // Restrict instead of Cascade to avoid SQL Server multiple-cascade-paths error
        // (Projects -> Tasks direct AND Projects -> KanbanBoards -> KanbanColumns -> Tasks).
        // Soft deletes handle child cleanup at the application layer.
        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.KanbanColumn)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.KanbanColumnId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(t => new { t.ProjectId, t.Status });
        builder.HasIndex(t => t.AssignedToEmployeeId);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
