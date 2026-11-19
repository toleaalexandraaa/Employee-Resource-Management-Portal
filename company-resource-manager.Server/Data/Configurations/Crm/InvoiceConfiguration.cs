using company_resource_manager.Server.Models.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Crm;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", "crm");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(i => i.SubTotal).HasPrecision(18, 2);
        builder.Property(i => i.TaxRate).HasPrecision(5, 4);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(10);

        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => new { i.ClientId, i.Status });

        builder.HasOne(i => i.Client)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
