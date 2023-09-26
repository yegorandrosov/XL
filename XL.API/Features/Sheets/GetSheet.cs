using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneOf;
using XL.API.Data;
using XL.API.Data.Models;
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

            if (sheetCells == null)
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
                async ([FromBody]Command command, IMediator mediator) =>
                {
                    var result = await mediator.Send(command);
                    return result.Match(
                        cell => Results.Ok(cell), 
                        _ => Results.NotFound());
                });
        }
    }
}