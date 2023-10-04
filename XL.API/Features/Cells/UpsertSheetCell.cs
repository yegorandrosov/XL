using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Carter;
using Carter.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using XL.API.Data;
using XL.API.Data.Models;
using XL.API.Data.Repositories;
using XL.API.Features.Expressions;
using XL.API.Models;

namespace XL.API.Features.Cells;

public sealed class UpsertSheetCell
{
    public record Command(string SheetId, string CellId, string Value) : IRequest<OneOf<CommandHelperClasses<SheetCell>, Unprocessable>>;

    public class Handler : IRequestHandler<Command, OneOf<CommandHelperClasses<SheetCell>, Unprocessable>>
    {
        private readonly ApplicationDbContext context;
        private readonly IMediator mediator;
        private readonly ISheetCellRepository sheetCellRepository;
        private readonly Dictionary<string, bool> cellValidationMap = new();

        public Handler(ApplicationDbContext context, IMediator mediator, ISheetCellRepository sheetCellRepository)
        {
            this.context = context;
            this.mediator = mediator;
            this.sheetCellRepository = sheetCellRepository;
        }

        public async Task<OneOf<CommandHelperClasses<SheetCell>, Unprocessable>> Handle(Command request, CancellationToken cancellationToken)
        {
            var expression = await mediator.Send(new ParseExpressionRequest(request.Value), cancellationToken);
            if (expression.IsError)
                return new Unprocessable();
            
            var evaluatedExpression =
                await mediator.Send(new EvaluateExpressionRequest(request.SheetId, expression), cancellationToken);
            if (evaluatedExpression.IsError)
                return new Unprocessable();
            
            var cell = await sheetCellRepository.Find(request.SheetId, request.CellId);
            if (cell != null)
            {
                var valid = await ValidateAndUpdateReferences(request, expression, cell, evaluatedExpression);

                if (!valid)
                    return new Unprocessable();
            }
            else
            {
                cell = await Create(request, evaluatedExpression);
            }
            
            await context.SaveChangesAsync(cancellationToken);

            return cell.Success();
        }

        private async Task<bool> ValidateAndUpdateReferences(Command request, Expression expression, SheetCell cell,
            Expression evaluatedExpression)
        {
            var containsCircularReference = expression.DependentVariables
                .Any(varName => cell.Callers.Any(c => c.Parent.CellId == varName) || varName == request.CellId);
            if (containsCircularReference)
            {
                return false;
            }

            try
            {
                await Update(cell, request.Value, evaluatedExpression);

                if (cellValidationMap.Any(x => x.Value == false))
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
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

        private async Task Update(SheetCell cell, string stringExpression, Expression expression)
        {
            cell.Expression = stringExpression;
            cell.NumericValue = expression.IsNumber ? expression.NumericValue : null;
            cell.Arguments.Clear();

            foreach (var depCellId in expression.DependentVariables)
            {
                var argumentCell = await sheetCellRepository.Find(cell.SheetId, depCellId);
                                
                cell.Arguments.Add(new SheetCellReference()
                {
                    Child = argumentCell,
                    Parent = cell
                });
            }

            await sheetCellRepository.Update(cell);
            context.SheetCells.Update(cell);

            await UpdateCallingExpressions(cell);
        }

        private async Task UpdateCallingExpressions(SheetCell cell)
        {
            foreach (var cellRef in cell.Callers)
            {
                var caller = cellRef.Parent;

                var parsedExpression = await mediator.Send(new ParseExpressionRequest(caller.Expression));
                if (parsedExpression.IsError)
                {
                    cellValidationMap[caller.CellId] = false;
                    continue;
                }
                var evaluatedExpression =
                    await mediator.Send(new EvaluateExpressionRequest(caller.SheetId, parsedExpression));
                if (evaluatedExpression.IsError)
                {
                    cellValidationMap[caller.CellId] = false;
                    continue;
                }

                if (caller.NumericValue != evaluatedExpression.NumericValue)
                {
                    await Update(caller, caller.Expression, evaluatedExpression);
                    cellValidationMap[caller.CellId] = true;
                }
            }
        }
    }

    public class Endpoint : ICarterModule
    {
        [SuppressMessage("ReSharper", "RouteTemplates.RouteParameterConstraintNotResolved")]
        [SuppressMessage("ReSharper", "RouteTemplates.RouteParameterIsNotPassedToMethod")]
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/v1/{sheetId}/{cellId}", 
                async ([FromRoute]string sheetId, [FromRoute]string cellId, [FromBody]CellApiRequest request, IMediator mediator, IMapper mapper) =>
            {
                var command = new Command(sheetId, cellId, request.Value);
                var result = await mediator.Send(command);

                return result.Match(success =>
                {
                    var responseObj = mapper.Map<CellApiResponse>(success.Value);
                    return Results.Created($"/api/v1/{command.SheetId}/{command.CellId}", responseObj);
                }, 
                _ => Results.UnprocessableEntity(new CellApiResponse()
                {
                    Value = command.Value,
                    Result = "ERROR"
                }));
            })
            .WithName(nameof(UpsertSheetCell))
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .IncludeInOpenApi();
        }
    }
}