using XL.API.Models;

namespace XL.API.Features.Expressions;

public record GetSheetCellValueQuery(string SheetId, string CellId) : IRequest<OneOf<string, double, NotFound>>;