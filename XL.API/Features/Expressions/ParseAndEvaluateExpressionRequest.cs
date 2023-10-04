// namespace XL.API.Features.Expressions;
//
// public record ParseAndEvaluateExpressionRequest(string SheetId, string Expression) : IRequest<OneOf<Expression, ParserError>>;
//
// public class
//     ParseAndEvaluateExpressionRequestHandler : IRequestHandler<ParseAndEvaluateExpressionRequest,
//         OneOf<Expression, ParserError>>
// {
//     private readonly IMediator mediator;
//
//     public ParseAndEvaluateExpressionRequestHandler(IMediator mediator)
//     {
//         this.mediator = mediator;
//     }
//
//     public async Task<OneOf<Expression, ParserError>> Handle(ParseAndEvaluateExpressionRequest request, CancellationToken cancellationToken)
//     {
//         var expression = await mediator.Send(new ParseExpressionRequest(request.Expression), cancellationToken);
//         var result = await mediator.Send(new EvaluateExpressionRequest(request.SheetId, expression), cancellationToken);
//
//         return result;
//     }
// }