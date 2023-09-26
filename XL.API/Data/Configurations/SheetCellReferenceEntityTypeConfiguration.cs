using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XL.API.Data.Models;

namespace XL.API.Data.Configurations;

public class SheetCellReferenceEntityTypeConfiguration : IEntityTypeConfiguration<SheetCellReference>
{
    public void Configure(EntityTypeBuilder<SheetCellReference> builder)
    {
        builder.HasKey(x => new { x.ParentId, x.ChildId });

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Arguments)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Child)
            .WithMany(x => x.Callers)
            .OnDelete(DeleteBehavior.NoAction);
    }
}