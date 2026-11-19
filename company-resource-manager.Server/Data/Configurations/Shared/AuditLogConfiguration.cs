using company_resource_manager.Server.Models.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Shared;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(256);
        builder.Property(a => a.EntityId).IsRequired().HasMaxLength(128);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValues).HasColumnType("nvarchar(max)");

        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.ChangedAt);
    }
}
