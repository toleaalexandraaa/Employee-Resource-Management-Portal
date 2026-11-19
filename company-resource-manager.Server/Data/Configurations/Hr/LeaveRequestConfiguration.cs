using company_resource_manager.Server.Models.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Hr;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests", "hr");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason).HasMaxLength(500);

        builder.HasOne(r => r.Employee)
            .WithMany(e => e.LeaveRequests)
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.EmployeeId, r.StartDate, r.EndDate });

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
