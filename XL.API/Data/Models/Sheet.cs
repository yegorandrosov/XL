namespace XL.API.Data.Models;

public class Sheet
{
    public Sheet(List<SheetCell> cells)
    {
        Cells = cells;
    }

    public List<SheetCell> Cells { get; }
}