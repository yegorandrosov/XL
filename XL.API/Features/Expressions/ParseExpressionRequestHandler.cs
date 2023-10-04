using System.Diagnostics;

namespace XL.API.Features.Expressions;

public record ParseExpressionRequest(string Expression) : IRequest<Expression>;
public class ParseExpressionRequestHandler : IRequestHandler<ParseExpressionRequest, Expression>
{
    public Task<Expression> Handle(ParseExpressionRequest request, CancellationToken cancellationToken)
    {
        Expression? root = null;
        try
        {
            var tokens = ParseTokens(request.Expression);
            
            root = ConvertTokensToExpression(tokens);
        }
        catch (Exception e)
        {
            root ??= new Expression();
            root.IsError = true;
        }

        return Task.FromResult(root);
    }
    
    private List<Token> ParseTokens(string expression)
    {
        var tokens = new List<Token>();
        Token? previous = null;
        var isFormula = expression[0] == '=';
        var lastNonWhiteSpaceIdx = -1;
            
        for (var i = 0; i < expression.Length; i++)
        {
            var charToken = expression[i];
            if (isFormula && charToken == ' ')
                continue;

            var tokenType = ParseTokenType(previous, charToken, lastNonWhiteSpaceIdx != i - 1);
            HandleUnexpectedTokenTypes(tokenType, i);

            var newToken = new Token(charToken, tokenType);

            tokens.Add(newToken);
            previous = newToken;
            
            if (!charToken.IsWhitespace())
                lastNonWhiteSpaceIdx = i;
        }

        if (!isFormula)
        {
            if (!decimal.TryParse(expression, out _))
            {
                tokens = tokens.Select(x => x with { Type = TokenType.Text }).ToList();
            }
        }

        return tokens;
    }

    private void HandleUnexpectedTokenTypes(TokenType tokenType, int i)
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

    private TokenType ParseTokenType(Token? previous, char token, bool afterWhitespace)
    {
        if (previous == null || previous.Type == TokenType.None)
        {
            if (token.IsFormulaSign())
                return TokenType.FormulaSign;

            if (char.IsDigit(token))
                return TokenType.Digit;

            if (token.IsMinus())
                return TokenType.Digit;
            
            return TokenType.Text;
        }

        if (previous.Type == TokenType.Digit)
        {
            if (char.IsDigit(token))
                return afterWhitespace ? TokenType.InvalidToken : TokenType.Digit;

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
            
            if (token.IsMinus())
                return TokenType.Digit;
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

            if (token.IsMinus())
                return TokenType.Digit;
        }

        return TokenType.InvalidToken;
    }

    private bool ExpressionMayContainMultipleTokens(TokenType type)
    {
        switch (type)
        {
            case TokenType.InvalidToken:
            case TokenType.None:
            case TokenType.BinaryOperand:
            case TokenType.OpeningParenthesis:
            case TokenType.ClosingParenthesis:
            case TokenType.FormulaSign:
                return false;
            case TokenType.Digit:
            case TokenType.Variable:
            case TokenType.Text:
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
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
                
                if (current.IsVariable)
                    root.DependentVariables.Add(current.StringValue);

                current = next;
                
            }
            else
            {
                current.Tokens.Add(tokens[i]);
            }
        }
        
        if (current.IsVariable)
            root.DependentVariables.Add(current.StringValue);

        return root;
    }

    
}