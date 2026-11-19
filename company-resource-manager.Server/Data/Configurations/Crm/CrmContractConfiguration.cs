using company_resource_manager.Server.Models.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Crm;

public class CrmContractConfiguration : IEntityTypeConfiguration<CrmContract>
{
    public void Configure(EntityTypeBuilder<CrmContract> builder)
    {
        builder.ToTable("Contracts", "crm");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ContractNumber).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Value).HasPrecision(18, 2);
        builder.Property(c => c.Currency).IsRequired().HasMaxLength(10);
        builder.Property(c => c.DocumentPath).HasMaxLength(1024);

        builder.HasIndex(c => c.ContractNumber).IsUnique();

        builder.HasOne(c => c.Client)
            .WithMany(cl => cl.Contracts)
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
