namespace XL.API.Features.Expressions;

public record ParseExpressionRequest(string SheetId, string Expression) : IRequest<OneOf<Expression, ParserError>>;