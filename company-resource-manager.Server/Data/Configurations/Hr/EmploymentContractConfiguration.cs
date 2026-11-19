using company_resource_manager.Server.Models.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Hr;

public class EmploymentContractConfiguration : IEntityTypeConfiguration<EmploymentContract>
{
    public void Configure(EntityTypeBuilder<EmploymentContract> builder)
    {
        builder.ToTable("EmploymentContracts", "hr");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.GrossSalary).HasPrecision(18, 2);
        builder.Property(c => c.Currency).IsRequired().HasMaxLength(10);

        builder.HasOne(c => c.Employee)
            .WithMany(e => e.Contracts)
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
