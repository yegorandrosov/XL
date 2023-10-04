using System.Diagnostics;
using System.Globalization;

namespace XL.API.Features.Expressions;

public record ParserError;

public record EvaluateExpressionRequest
    (string SheetId, Expression Expression) : IRequest<OneOf<Expression, ParserError>>;
public class EvaluateExpressionRequestHandler : IRequestHandler<EvaluateExpressionRequest, OneOf<Expression, ParserError>>
{
    private readonly IMediator mediator;

    public EvaluateExpressionRequestHandler(IMediator mediator)
    {
        this.mediator = mediator;
    }
        
    public async Task<OneOf<Expression, ParserError>> Handle(EvaluateExpressionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var root = request.Expression;
        
            if (root.IsNumber || root.IsText)
            {
                return root;
            }

            if (root.IsFormula)
            {
                await ReplaceVariablesFromContext(request.SheetId, root);
                ReplaceWhitespaceExpressions(root);
                ReplaceNestedExpressions(root);
                var newRoot = TryCalculateExpression(root.Next);

                newRoot.DependentVariables = root.DependentVariables;
                return newRoot;
            }
        }
        catch (Exception)
        {
            // ignored
        }
        
        return new ParserError();
    }

    private async Task ReplaceVariablesFromContext(string sheetId, Expression root)
    {
        var current = root.Next;

        do
        {
            if (!current!.IsVariable)
            {
                current = current.Next;
                continue;
            }
                
            var value = await mediator.Send(new GetSheetCellValueQuery(sheetId, current.StringValue));

            if (!value.TryPickT1(out var numericValue, out _))
            {
                throw new ExpressionParserException();
            }
            
            var newExpression = new Expression()
            {
                Tokens = numericValue.ToString(CultureInfo.InvariantCulture).Select(x => new Token(x, TokenType.Digit)).ToList(),
                Next = current.Next,
                Previous = current.Previous,
            };

            current.Previous!.Next = newExpression;
            if (current.Next != null)
                current.Next.Previous = newExpression;

            current = newExpression.Next;
        } while (current != null);
    }

    private Expression TryCalculateExpression(Expression exp)
    {
        if (exp is null)
            throw new ArgumentNullException(nameof(exp));
            
        var start = exp;
        var current = exp;
        Expression? highPrio;
        int opCount;
            
        do
        {
            highPrio = null;
            opCount = FindNextHighPriorityOperation(current, ref highPrio);

            if (opCount == 0 || highPrio == null)
                return start;
                
            var previousArg = highPrio.Previous;
            var nextArg = highPrio.Next;

            var newExpr = CalculateAndReplaceHighPriorityExpression(highPrio, previousArg, nextArg);

            opCount--;
            if (previousArg == start)
            {
                start = newExpr;
            }
            current = start;
        } while (opCount > 0);

        return start;
    }

    private int FindNextHighPriorityOperation(Expression current, ref Expression? highPrio)
    {
        var opCount = 0;
        do
        {
            if (current.IsBinaryOperand)
            {
                opCount++;
                if (highPrio is not { IsHighPrioOperand: true })
                {
                    highPrio = current;
                }
            }

            current = current.Next;
        } while (current is { IsClosingParentheses: false });

        return opCount;
    }

    private Expression CalculateAndReplaceHighPriorityExpression(Expression highPrio, Expression? previousArg,
        Expression? nextArg)
    {
        var result = highPrio.Tokens[0].Value switch
        {
            '*' => previousArg.NumericValue * nextArg.NumericValue,
            '/' => previousArg.NumericValue / nextArg.NumericValue,
            '-' => previousArg.NumericValue - nextArg.NumericValue,
            '+' => previousArg.NumericValue + nextArg.NumericValue,
            _ => throw new ArgumentException(),
        };

        var newExpr = new Expression()
        {
            Tokens = result.ToString(CultureInfo.InvariantCulture).Select(x => new Token(x, TokenType.Digit)).ToList()
        };

        newExpr.Previous = previousArg.Previous.IsOpeningParentheses ? previousArg.Previous.Previous : previousArg.Previous;

        previousArg.Previous.Next = newExpr;

        if (nextArg.Next != null)
        {
            nextArg.Next.Previous = newExpr;
            newExpr.Next = nextArg.Next.IsClosingParentheses ? nextArg.Next.Next : nextArg.Next;
        }

        return newExpr;
    }

    private void ReplaceNestedExpressions(Expression exp)
    {
        var current = exp;
        var replaced = false;

        var map = new List<(int, Expression)>();

        var level = 0;
        do
        {
            if (current.IsOpeningParentheses)
            {
                level++;
                map.Add((level, current));
            }

            if (current.IsClosingParentheses)
            {
                level--;
            }
                
            current = current.Next;
        } while (current != null);

        foreach (var expression in map.OrderByDescending(x => x.Item1))
        {
            ReplaceNestedExpression(expression.Item2);
        }
    }

    private void ReplaceNestedExpression(Expression current)
    {
        do
        {
            if (current.IsOpeningParentheses)
            {
                var nestedExpr = TryCalculateExpression(current.Next);
                nestedExpr.Next = current.Next.Next; // +1 next for closing parenthesis
                nestedExpr.Previous = current.Previous;

                current.Previous!.Next = nestedExpr;
                if (current.Next.Next != null)
                    current.Next.Next.Previous = nestedExpr;
            }

            current = current.Next;
        } while (current is { IsClosingParentheses: false });
    }

    private void ReplaceWhitespaceExpressions(Expression current)
    {
        do
        {
            if (current.IsWhitespace)
            {
                current.Previous!.Next = current.Next;
                if (current.Next != null)
                    current.Next.Previous = current.Previous;
            }

            current = current.Next;
        } while (current is not null);
    }

}