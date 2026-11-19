using company_resource_manager.Server.Models.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Hr;

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("LeaveBalances", "hr");

        builder.HasKey(b => b.Id);

        builder.HasIndex(b => new { b.EmployeeId, b.Year, b.LeaveType }).IsUnique();

        builder.HasOne(b => b.Employee)
            .WithMany(e => e.LeaveBalances)
            .HasForeignKey(b => b.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
