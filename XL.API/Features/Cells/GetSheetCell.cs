using AutoMapper;
using Carter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XL.API.Data;
using XL.API.Data.Models;
using XL.API.Models;

// ReSharper disable UnusedType.Global
// ReSharper disable ConvertClosureToMethodGroup
// ReSharper disable RouteTemplates.RouteParameterConstraintNotResolved
// ReSharper disable RouteTemplates.RouteParameterIsNotPassedToMethod

namespace XL.API.Features.Cells;

public static class GetSheetCell
{
    public record Command(string SheetId, string CellId) : IRequest<OneOf<CommandHelperClasses<SheetCell>, NotFound>>;

    public class Handler : IRequestHandler<Command, OneOf<CommandHelperClasses<SheetCell>, NotFound>>
    {
        private readonly ApplicationDbContext context;

        public Handler(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<OneOf<CommandHelperClasses<SheetCell>, NotFound>> Handle(Command request, CancellationToken cancellationToken)
        {
            var sheetCell = await context.SheetCells.FirstOrDefaultAsync(x => x.SheetId == request.SheetId && x.CellId == request.CellId, cancellationToken: cancellationToken);

            if (sheetCell == null)
            {
                return new NotFound();
            }

            return sheetCell.Success();
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/v1/{sheetId}/{cellId}", 
                async ([FromBody]Command command, IMediator mediator, IMapper mapper) =>
                {
                    var result = await mediator.Send(command);
                    return result.Match(
                        cell => Results.Ok(mapper.Map<CellApiResponse>(cell.Value)), 
                        _ => Results.NotFound());
                });
        }
    }
}