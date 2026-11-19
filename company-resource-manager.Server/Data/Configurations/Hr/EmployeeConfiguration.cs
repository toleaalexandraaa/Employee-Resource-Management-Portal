using company_resource_manager.Server.Models.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Hr;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", "hr");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PersonalEmail).IsRequired().HasMaxLength(256);
        builder.Property(e => e.WorkEmail).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Phone).HasMaxLength(30);
        builder.Property(e => e.Department).IsRequired().HasMaxLength(100);
        builder.Property(e => e.JobTitle).IsRequired().HasMaxLength(150);

        builder.HasIndex(e => e.WorkEmail).IsUnique();

        // Self-referencing manager relationship
        builder.HasOne(e => e.Manager)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
