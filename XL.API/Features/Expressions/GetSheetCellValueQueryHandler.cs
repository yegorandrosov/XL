using XL.API.Data.Cache;
using XL.API.Models;

namespace XL.API.Features.Expressions;

public record GetSheetCellValueQuery(string SheetId, string CellId) : IRequest<OneOf<string, double, NotFound>>;
public class GetSheetCellValueQueryHandler : IRequestHandler<GetSheetCellValueQuery, OneOf<string, double, NotFound>>
{
    private readonly ISheetCellRepository repository;

    public GetSheetCellValueQueryHandler(ISheetCellRepository repository)
    {
        this.repository = repository;
    }
    
    public async Task<OneOf<string, double, NotFound>> Handle(GetSheetCellValueQuery request, CancellationToken cancellationToken)
    {
        var cell = await repository.Find(request.SheetId, request.CellId);
        if (cell == null)
            return new NotFound();

        if (cell.NumericValue.HasValue)
            return cell.NumericValue.Value;

        return cell.Expression;
    }
}