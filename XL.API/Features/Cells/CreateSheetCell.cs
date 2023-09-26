using System.Diagnostics.CodeAnalysis;
using Carter;
using Carter.OpenApi;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OneOf;
using XL.API.Data;
using XL.API.Data.Models;
using XL.API.Models;

namespace XL.API.Features.Cells;

// TODO: add fluent validator

public sealed class CreateSheetCell
{
    public record Command(string SheetId, string CellId, string Value) : IRequest<OneOf<Success<SheetCell>, AlreadyExists<SheetCell>>>;

    public class Handler : IRequestHandler<Command, OneOf<Success<SheetCell>, AlreadyExists<SheetCell>>>
    {
        private readonly ApplicationDbContext context;
        private readonly IMediator mediator;

        public Handler(ApplicationDbContext context, IMediator mediator)
        {
            this.context = context;
            this.mediator = mediator;
        }

        public async Task<OneOf<Success<SheetCell>, AlreadyExists<SheetCell>>> Handle(Command request, CancellationToken cancellationToken)
        {
            SheetCell cell;
            try
            {
                cell = context.SheetCells.Add(new SheetCell()
                {
                    SheetId = request.SheetId,
                    CellId = request.CellId,
                    Value = request.Value,
                    Expression = request.Value,
                    Level = 1
                }).Entity;

                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlException && sqlException.Number == 2601)
                {
                    var response = await mediator.Send(new GetSheetCell.Command(request.SheetId, request.CellId), cancellationToken);

                    if (response.TryPickT1(out _, out var success))
                    {
                        return success.Value.AlreadyExists();
                    }
                }

                throw;
            }

            return cell.Success();
        }
    }

    public class Endpoint : ICarterModule
    {
        [SuppressMessage("ReSharper", "RouteTemplates.RouteParameterConstraintNotResolved")]
        [SuppressMessage("ReSharper", "RouteTemplates.RouteParameterIsNotPassedToMethod")]
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/v1/{sheetId}/{cellId}", 
                async ([FromBody]Command command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);

                return result.Match(success => Results.Ok(success.Value), 
                    alreadyExists => Results.Conflict(alreadyExists.Value));
                
            })
            .WithName(nameof(CreateSheetCell))
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status200OK)
            .IncludeInOpenApi();
        }
    }
}