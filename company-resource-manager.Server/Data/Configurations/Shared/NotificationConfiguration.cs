using company_resource_manager.Server.Models.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Shared;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).IsRequired().HasMaxLength(256);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(1000);

        builder.HasQueryFilter(n => !n.IsDeleted);

        builder.HasIndex(n => n.RecipientUserId);
        builder.HasIndex(n => n.IsRead);
    }
}
