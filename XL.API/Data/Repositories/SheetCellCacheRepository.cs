using Microsoft.EntityFrameworkCore;
using XL.API.Data.Models;

namespace XL.API.Data.Repositories;

public class SheetCellCacheRepository : ISheetCellRepository
{
    private readonly ApplicationDbContext context;
    private Dictionary<string, SheetCell> cache = new();

    public SheetCellCacheRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<SheetCell?> Find(string sheetId, string cellId)
    {
        var cacheKey = $"{sheetId}/{cellId}";
        cache.TryGetValue(cacheKey, out var cell);
        if (cell != null)
            return cell;

        cell = await context.SheetCells
            .Include(x => x.Arguments)
            .Include(x => x.Callers)
            .FirstOrDefaultAsync(x => x.SheetId == sheetId && x.CellId == cellId);
        cache[cacheKey] = cell;
        return cell;
    }

    public Task Update(SheetCell sheetCell)
    {
        var cacheKey = $"{sheetCell.SheetId}/{sheetCell.CellId}";
        cache[cacheKey] = sheetCell;

        return Task.CompletedTask;
    }
}