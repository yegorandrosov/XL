using System.Diagnostics;
using System.Globalization;
using MediatR;
using OneOf;

namespace XL.API.Features.Parser;

public record ParserError;

public record ParseExpressionRequest(string SheetId, string Expression) : IRequest<OneOf<Expression, ParserError>>;
public class ParseExpressionRequestHandler : IRequestHandler<ParseExpressionRequest, OneOf<Expression, ParserError>>
{
    private readonly IMediator mediator;

    public ParseExpressionRequestHandler(IMediator mediator)
    {
        this.mediator = mediator;
    }
        
    public async Task<OneOf<Expression, ParserError>> Handle(ParseExpressionRequest request, CancellationToken cancellationToken)
    {
        var tokens = ParseTokens(request.Expression);
        var root = ConvertTokensToExpression(tokens);
        try
        {
            if (root.IsNumber)
            {
                return root;
            }

            if (root.IsFormula)
            {
                await ReplaceVariablesFromContext(request.SheetId, root);

                ReplaceNestedExpressions(root);
                    
                return TryCalculateExpression(root.Next);
            }
        }
        catch (Exception)
        {
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
        Expression? highPrio;  // operation that must be executed first
        int opCount;
            
        do
        {
            opCount = 0;
            highPrio = null;
                
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

            if (opCount == 0 || highPrio == null)
                return start;
                
            // calculate high prio
            var previousArg = highPrio.Previous;
            var nextArg = highPrio.Next;

            var newExpr = CalculateAndReplaceHighPriorityExpression(highPrio, previousArg, nextArg);

            opCount--;
            if (previousArg == start)
            {
                start = newExpr;
            }
            current = start;
                
            Debug.WriteLine(start);

        } while (opCount > 0);

        return start;
    }

    private static Expression CalculateAndReplaceHighPriorityExpression(Expression highPrio, Expression? previousArg,
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
            Tokens = result.ToString().Select(x => new Token(x, TokenType.Digit)).ToList()
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

    private Expression ConvertTokensToExpression(List<Token> tokens)
    {
        var root = new Expression();
        var current = root;
        current.Tokens.Add(tokens[0]);
            
        for (var i = 1; i < tokens.Count; i++)
        {
            if (tokens[i].Type != tokens[i - 1].Type ||
                !ExpressionMayContainMultipleTokens(tokens[i].Type))
            {
                var next = new Expression();
                next.Previous = current;
                next.Tokens.Add(tokens[i]);

                current.Next = next;

                current = next;
            }
            else
            {
                current.Tokens.Add(tokens[i]);
            }
        }

        return root;
    }

    private List<Token> ParseTokens(string expression)
    {
        var tokens = new List<Token>();
        Token? previous = null;
        var isFormula = expression[0] == '=';
            
        for (var i = 0; i < expression.Length; i++)
        {
            var charToken = expression[i];

            var tokenType = ParseTokenType(previous, charToken);
            HandleUnexpectedTokenTypes(tokenType, i, isFormula);

            var newToken = new Token(charToken, tokenType);

            tokens.Add(newToken);

            previous = newToken;
        }

        if (!isFormula)
        {
            if (!double.TryParse(expression, out _))
            {
                tokens = tokens.Select(x => x with { Type = TokenType.Text }).ToList();
            }
        }

        return tokens;
    }

    private static void HandleUnexpectedTokenTypes(TokenType tokenType, int i, bool isFormula)
    {
        if (tokenType == TokenType.InvalidToken)
        {
            throw new ExpressionParserException(i);
        }

        if (tokenType == TokenType.FormulaSign && i != 0)
        {
            throw new ExpressionParserException(i);
        }
    }

    private TokenType ParseTokenType(Token? previous, char token)
    {
        if (previous == null || previous.Type == TokenType.None)
        {
            if (token.IsFormulaSign())
                return TokenType.FormulaSign;

            if (char.IsDigit(token))
                return TokenType.Digit;

            return TokenType.Text;
        }

        if (previous.Type == TokenType.Digit)
        {
            if (char.IsDigit(token))
                return TokenType.Digit;

            if (token.IsBinaryOperand())
                return TokenType.BinaryOperand;

            if (token == '.')
                return TokenType.Digit;

            if (token.IsClosingParenthesis())
                return TokenType.ClosingParenthesis;
                
            return TokenType.Text;
        }

        if (previous.Type == TokenType.BinaryOperand)
        {
            if (char.IsDigit(token))
                return TokenType.Digit;
                
            if (token.IsText())
                return TokenType.Variable;

            if (token.IsOpeningParenthesis())
                return TokenType.OpeningParenthesis;
        }
        else if (previous.Type == TokenType.FormulaSign)
        {
            if (char.IsDigit(token))
                return TokenType.Digit;
                
            if (token.IsText())
                return TokenType.Variable;
                
            if (token.IsOpeningParenthesis())
                return TokenType.OpeningParenthesis;
        }
        else if (previous.Type == TokenType.Variable)
        {
            if (char.IsDigit(token))
                return TokenType.Variable;

            if (token.IsText())
                return TokenType.Variable;
                
            if (token.IsBinaryOperand())
                return TokenType.BinaryOperand;
                
            if (token.IsClosingParenthesis())
                return TokenType.ClosingParenthesis;
        }
        else if (previous.Type == TokenType.Text)
        {
            return TokenType.Text;
        }
        else if (previous.Type == TokenType.ClosingParenthesis)
        {
            if (token.IsBinaryOperand())
                return TokenType.BinaryOperand;
                
            if (token.IsClosingParenthesis())
                return TokenType.ClosingParenthesis;
        }
        else if (previous.Type == TokenType.OpeningParenthesis)
        {
            if (token.IsOpeningParenthesis())
                return TokenType.OpeningParenthesis;

            if (char.IsDigit(token))
                return TokenType.Digit;

            if (token.IsText())
                return TokenType.Variable;
        }

        return TokenType.InvalidToken;
    }

    private bool ExpressionMayContainMultipleTokens(TokenType type)
    {
        return type switch
        {
            TokenType.InvalidToken => false,
            TokenType.None => false,
            TokenType.BinaryOperand => false,
            TokenType.Digit => true,
            TokenType.FormulaSign => false,
            TokenType.Variable => true,
            TokenType.Text => true,
            TokenType.OpeningParenthesis => false,
            TokenType.ClosingParenthesis => false,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}

public static class CharExtensions
{
    public static bool IsBinaryOperand(this char ch)
    {
        switch (ch)
        {
            case '+':
            case '-':
            case '*':
            case '/':
                return true;
        }

        return false;
    }

    public static bool IsFormulaSign(this char ch)
    {
        return ch == '=';
    }

    public static bool IsText(this char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }

    public static bool IsClosingParenthesis(this char c)
    {
        return c is ')';
    }

    public static bool IsOpeningParenthesis(this char c)
    {
        return c is '(';
    }
}