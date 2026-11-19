using company_resource_manager.Server.Models.Proj;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace company_resource_manager.Server.Data.Configurations.Proj;

public class KanbanBoardConfiguration : IEntityTypeConfiguration<KanbanBoard>
{
    public void Configure(EntityTypeBuilder<KanbanBoard> builder)
    {
        builder.ToTable("KanbanBoards", "proj");

        builder.HasKey(b => b.Id);

        builder.HasOne(b => b.Project)
            .WithOne(p => p.KanbanBoard)
            .HasForeignKey<KanbanBoard>(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.ProjectId).IsUnique();

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
