using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using XL.API.Data.Models;

namespace XL.API.Data.Configurations;

public class SheetCellEntityTypeConfiguration : IEntityTypeConfiguration<SheetCell>
{
    public void Configure(EntityTypeBuilder<SheetCell> builder)
    {
        builder.HasKey(x => new { x.SheetId, x.Reference });
    }
}