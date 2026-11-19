using company_resource_manager.Server.Models.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Crm;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads", "crm");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.CompanyName).IsRequired().HasMaxLength(256);
        builder.Property(l => l.ContactName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.ContactEmail).IsRequired().HasMaxLength(256);
        builder.Property(l => l.ContactPhone).HasMaxLength(30);
        builder.Property(l => l.Notes).HasMaxLength(2000);
        builder.Property(l => l.EstimatedValue).HasPrecision(18, 2);

        builder.HasIndex(l => new { l.Stage, l.AssignedToUserId });

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
