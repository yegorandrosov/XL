using System.Diagnostics.CodeAnalysis;
using Carter;
using Carter.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XL.API.Data;
using XL.API.Data.Cache;
using XL.API.Data.Models;
using XL.API.Features.Expressions;
using XL.API.Models;

namespace XL.API.Features.Cells;

public sealed class CreateSheetCell
{
    public record Command(string SheetId, string CellId, string Value) : IRequest<OneOf<Success<SheetCell>, AlreadyExists<SheetCell>,Unprocessable>>;

    public class Handler : IRequestHandler<Command, OneOf<Success<SheetCell>, AlreadyExists<SheetCell>, Unprocessable>>
    {
        private readonly ApplicationDbContext context;
        private readonly IMediator mediator;
        private readonly ISheetCellRepository sheetCellRepository;

        public Handler(ApplicationDbContext context, IMediator mediator, ISheetCellRepository sheetCellRepository)
        {
            this.context = context;
            this.mediator = mediator;
            this.sheetCellRepository = sheetCellRepository;
        }

        public async Task<OneOf<Success<SheetCell>, AlreadyExists<SheetCell>, Unprocessable>> Handle(Command request, CancellationToken cancellationToken)
        {
            var expression = await mediator.Send(new ParseExpressionRequest(request.Value), cancellationToken);
            if (expression.IsError)
                return new Unprocessable();
            
            var cell = await sheetCellRepository.Find(request.SheetId, request.CellId);
            if (cell != null)
            {if (expression.DependentVariables.Any(cell.))
                Update(cell, request.Value, expression);
            }
            else
            {
                cell = await Create(request, expression);
            }
            
            await context.SaveChangesAsync(cancellationToken);

            return cell.Success();
        }

        private async Task<SheetCell> Create(Command request, Expression expression)
        {
            var cell = new SheetCell()
            {
                SheetId = request.SheetId,
                CellId = request.CellId,
                Expression = request.Value,
                NumericValue = expression.IsNumber ? expression.NumericValue : null,
            };

            foreach (var cellId in expression.DependentVariables)
            {
                var argumentCell = await sheetCellRepository.Find(request.SheetId, cellId);
                
                cell.Arguments.Add(new SheetCellReference()
                {
                    Child = argumentCell,
                    Parent = cell
                });
            }

            context.SheetCells.Add(cell);
            
            return cell;
        }

        private void Update(SheetCell cell, string stringExpression, Expression parsedExpression)
        {
            
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
                    alreadyExists => Results.Conflict(alreadyExists.Value),
                    _ => Results.UnprocessableEntity());
            })
            .WithName(nameof(CreateSheetCell))
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .IncludeInOpenApi();
        }
    }
}