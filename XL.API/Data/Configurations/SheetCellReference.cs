using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XL.API.Data.Models;

namespace XL.API.Data.Configurations;

public class SheetCellReferenceEntityTypeConfiguration : IEntityTypeConfiguration<SheetCellReference>
{
    public void Configure(EntityTypeBuilder<SheetCellReference> builder)
    {
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Arguments);

        builder.HasOne(x => x.Child)
            .WithMany(x => x.Callers);
    }
}