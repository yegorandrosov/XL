using XL.API.Data.Models;

namespace XL.API.Data.Repositories;

public interface ISheetCellRepository
{
    Task<SheetCell?> Find(string sheetId, string cellId);
    Task Update(SheetCell sheetCell);
}