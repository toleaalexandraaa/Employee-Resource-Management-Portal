using company_resource_manager.Server.Models.Proj;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Proj;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers", "proj");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role).IsRequired().HasMaxLength(100);
        builder.Property(m => m.AllocatedHoursPerWeek).HasPrecision(5, 2);

        builder.HasOne(m => m.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ProjectId, m.EmployeeId });

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
