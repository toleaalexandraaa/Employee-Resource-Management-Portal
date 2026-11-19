using company_resource_manager.Server.Models.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Crm;

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems", "crm");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.Description).IsRequired().HasMaxLength(500);
        builder.Property(li => li.Quantity).HasPrecision(10, 2);
        builder.Property(li => li.UnitPrice).HasPrecision(18, 2);
        builder.Property(li => li.LineTotal).HasPrecision(18, 2);

        builder.HasOne(li => li.Invoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
