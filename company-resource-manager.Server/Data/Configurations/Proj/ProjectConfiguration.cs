using company_resource_manager.Server.Models.Proj;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Proj;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", "proj");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(256);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.EstimatedBudget).HasPrecision(18, 2);
        builder.Property(p => p.ActualBudget).HasPrecision(18, 2);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
