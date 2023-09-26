namespace XL.API.Data.Models;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class SheetCellReference
{
    public int ParentId { get; set; }
    public int ChildId { get; set; }

    public virtual SheetCell Parent { get; set; } = null!;
    public virtual SheetCell Child { get; set; } = null!;
}