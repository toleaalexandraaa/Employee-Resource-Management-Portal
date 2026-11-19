using company_resource_manager.Server.Models.Proj;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Proj;

public class KanbanColumnConfiguration : IEntityTypeConfiguration<KanbanColumn>
{
    public void Configure(EntityTypeBuilder<KanbanColumn> builder)
    {
        builder.ToTable("KanbanColumns", "proj");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(c => c.Board)
            .WithMany(b => b.Columns)
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
