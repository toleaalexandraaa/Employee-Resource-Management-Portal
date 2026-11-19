using company_resource_manager.Server.Models.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Hr;

public class PayrollRecordConfiguration : IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> builder)
    {
        builder.ToTable("PayrollRecords", "hr");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.GrossSalary).HasPrecision(18, 2);
        builder.Property(p => p.BonusAmount).HasPrecision(18, 2);
        builder.Property(p => p.MealTicketValue).HasPrecision(18, 2);
        builder.Property(p => p.NetSalary).HasPrecision(18, 2);
        builder.Property(p => p.TaxDeductions).HasPrecision(18, 2);
        builder.Property(p => p.SocialContributions).HasPrecision(18, 2);

        builder.HasIndex(p => new { p.EmployeeId, p.Year, p.Month }).IsUnique();

        builder.HasOne(p => p.Employee)
            .WithMany(e => e.PayrollRecords)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
