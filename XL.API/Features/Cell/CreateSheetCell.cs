using System.Diagnostics.CodeAnalysis;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using XL.API.Data;
using XL.API.Data.Models;

namespace XL.API.Features.Cell;

// TODO: add fluent validator

public static class CreateSheetCell
{
    public record Command(string SheetId, string CellId, string Value) : IRequest;

    public class Handler : IRequestHandler<Command>
    {
        private readonly ApplicationDbContext context;

        public Handler(ApplicationDbContext context)
        {
            this.context = context;
        }

        public Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            context.SheetCells.Add(new SheetCell()
            {
                SheetId = request.SheetId,
                CellId = request.CellId,
                Value = request.Value
            });
            context.SaveChanges();

            return Task.FromResult(default(Unit));
        }
    }

    public class Endpoint : ICarterModule
    {
        [SuppressMessage("ReSharper", "RouteTemplates.RouteParameterConstraintNotResolved")]
        [SuppressMessage("ReSharper", "RouteTemplates.RouteParameterIsNotPassedToMethod")]
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/v1/{sheetId:string}/{cellId:string}", 
                async ([FromBody]Command command, IMediator mediator) =>
            {
                await mediator.Send(command);
            });
        }
    }
}