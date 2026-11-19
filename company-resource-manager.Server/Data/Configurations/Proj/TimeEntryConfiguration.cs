using company_resource_manager.Server.Models.Proj;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Proj;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("TimeEntries", "proj");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.HoursLogged).HasPrecision(5, 2);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Task)
            .WithMany(t => t.TimeEntries)
            .HasForeignKey(e => e.TaskId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Project)
            .WithMany(p => p.TimeEntries)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(e => new { e.EmployeeId, e.Date });
        builder.HasIndex(e => e.ProjectId);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
