using Microsoft.EntityFrameworkCore;
using XL.API.Data.Models;

namespace XL.API.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<SheetCell> SheetCells { get; set; } = null!;
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}