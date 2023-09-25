namespace XL.API.Data.Models;

public class SheetCell
{
    public SheetCell(string sheetId, string reference, string value)
    {
        SheetId = sheetId;
        Reference = reference;
        Value = value;
    }

    public string SheetId { get; set; }
    
    public string Reference { get; set; }
    
    public string Value { get; set; }
}

