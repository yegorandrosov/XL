using System.Diagnostics.CodeAnalysis;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XL.API.Data;
using XL.API.Data.Models;
using XL.API.Models;
using OneOf;

namespace XL.API.Features.Cell;

public static class GetSheetCell
{
    public record Command(string SheetId, string CellId) : IRequest<OneOf<SheetCell, NotFound>>;

    public class Handler : IRequestHandler<Command, OneOf<SheetCell, NotFound>>
    {
        private readonly ApplicationDbContext context;

        public Handler(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<OneOf<SheetCell, NotFound>> Handle(Command request, CancellationToken cancellationToken)
        {
            var sheetCell = await context.SheetCells.FirstOrDefaultAsync(x => x.SheetId == request.SheetId && x.CellId == request.CellId, cancellationToken: cancellationToken);

            if (sheetCell == null)
            {
                return new NotFound();
            }

            return sheetCell;
        }
    }

    public class Endpoint : ICarterModule
    {
        [SuppressMessage("ReSharper", "RouteTemplates.RouteParameterConstraintNotResolved")]
        [SuppressMessage("ReSharper", "RouteTemplates.RouteParameterIsNotPassedToMethod")]
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("api/v1/{sheetId:string}/{cellId:string}", 
                async ([FromBody]Command command, IMediator mediator) =>
                {
                    return (await mediator.Send(command)).Switch(cell => );
                });
        }
    }
}