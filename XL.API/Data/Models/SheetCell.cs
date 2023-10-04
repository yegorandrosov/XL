namespace XL.API.Data.Models;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class SheetCell
{
    public int Id { get; set; }
    public string SheetId { get; set; } = null!;
    public string CellId { get; set; } = null!;
    public string Expression { get; set; } = null!;
    public decimal? NumericValue { get; set; } 
    public virtual ICollection<SheetCellReference> Callers { get; set; } = new List<SheetCellReference>();
    public virtual ICollection<SheetCellReference> Arguments { get; set; } = new List<SheetCellReference>();
}