﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using XL.API.Data;
using XL.API.Models;
using OneOf;

namespace XL.API.Features.Parser;

public interface ISheetContext
{
    double GetCellValue(string sheet, string value);
}


public record GetSheetCellValueQuery(string SheetId, string CellId) : IRequest<OneOf<string, double, NotFound>>;

public class GetSheetCellValueQueryHandler : IRequestHandler<GetSheetCellValueQuery, OneOf<string, double, NotFound>>
{
    private readonly ApplicationDbContext context;

    public GetSheetCellValueQueryHandler(ApplicationDbContext context)
    {
        this.context = context;
    }
    
    public async Task<OneOf<string, double, NotFound>> Handle(GetSheetCellValueQuery request, CancellationToken cancellationToken)
    {
        var cell = await context.SheetCells
            .FirstOrDefaultAsync(x => x.CellId == request.CellId && x.SheetId == request.SheetId, cancellationToken: cancellationToken);

        if (cell == null)
            return new NotFound();

        if (cell.NumericValue.HasValue)
            return cell.NumericValue.Value;

        return cell.Expression;
    }
}