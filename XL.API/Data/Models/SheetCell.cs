namespace XL.API.Data.Models;

public class SheetCell
{
    public int Id { get; set; }
    public string SheetId { get; set; }
    public string CellId { get; set; }
    public string Expression { get; set; }
    public string Value { get; set; }
    public int Level { get; set; }
    
    public virtual ICollection<SheetCellReference> Callers { get; set; }
    public virtual ICollection<SheetCellReference> Arguments { get; set; }
}

public class SheetCellReference
{
    public int ParentId { get; set; }
    public int ChildId { get; set; }

    public virtual SheetCell Parent { get; set; }
    
    public virtual SheetCell Child { get; set; }
}

