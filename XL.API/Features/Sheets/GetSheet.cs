using AutoMapper;
using Carter;
using Carter.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XL.API.Data;
using XL.API.Data.Models;
using XL.API.Features.Cells;
using XL.API.Models;
// ReSharper disable ConvertClosureToMethodGroup
// ReSharper disable RouteTemplates.RouteParameterConstraintNotResolved
// ReSharper disable RouteTemplates.RouteParameterIsNotPassedToMethod

namespace XL.API.Features.Sheets;

public class GetSheet
{
    public record Command(string SheetId) : IRequest<OneOf<Sheet, NotFound>>;

    public class Handler : IRequestHandler<Command, OneOf<Sheet, NotFound>>
    {
        private readonly ApplicationDbContext context;

        public Handler(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<OneOf<Sheet, NotFound>> Handle(Command request, CancellationToken cancellationToken)
        {
            var sheetCells = await context.SheetCells
                .Where(x => x.SheetId == request.SheetId)
                .ToListAsync(cancellationToken: cancellationToken);

            if (!sheetCells.Any())
            {
                return new NotFound();
            }

            var sheet = new Sheet(sheetCells);

            return sheet;
        }
    }

    public class Endpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/v1/{sheetId}", 
                async ([FromRoute]string sheetId, IMediator mediator, IMapper mapper) =>
            {
                var command = new Command(sheetId);
                var result = await mediator.Send(command);
                return result.Match(
                    cell =>
                    {
                        var map = cell.Cells.ToDictionary(x => x.CellId, x => mapper.Map<CellApiResponse>(x));
                        return Results.Ok(map);
                    }, 
                    _ => Results.NotFound());
            })
            .WithName(nameof(GetSheet))
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .IncludeInOpenApi();
        }
    }
}