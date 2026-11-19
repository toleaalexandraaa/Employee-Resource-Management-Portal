using company_resource_manager.Server.Models.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Crm;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients", "crm");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CompanyName).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Industry).HasMaxLength(100);
        builder.Property(c => c.Website).HasMaxLength(512);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Country).HasMaxLength(100);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
