using System.Text;

namespace XL.API
{
    public enum TokenType
    {
        InvalidToken = -1,
        None = 0,
        BinaryOperand,
        Zero,
        Digit,
        Formula,
    }

    public record Token(char Value, TokenType Type);

    public class Expression
    {
        public Expression? Previous { get; set; }
        public Expression? Next { get; set; }

        public List<Token> Tokens { get; set; } = new List<Token>();

        public string Value
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var c in Tokens.Select(x => x.Value))
                {
                    sb.Append(c);
                }

                return sb.ToString();
            }
        }
        
        public IEnumerable<Expression> AsEnumerable()
        {
            var current = this;

            do
            {
                yield return current;

                current = current.Next;
            } while (current != null);
        }
    }

    public class ExpressionParser
    {
        public Expression ParseExpression(string expression)
        {
            var tokens = ParseTokens(expression);
            var root = ConvertTokensToExpression(tokens);

            return root;
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

        public List<Token> ParseTokens(string expression)
        {
            var tokens = new List<Token>();
            Token? previous = null;

            for (var i = 0; i < expression.Length; i++)
            {
                var charToken = expression[i];

                var tokenType = ParseTokenType(previous, charToken);

                if (tokenType == TokenType.InvalidToken)
                {
                    throw new ExpressionParserException(i);
                }

                var newToken = new Token(charToken, tokenType);

                tokens.Add(newToken);

                previous = newToken;
            }

            return tokens;
        }

        public TokenType ParseTokenType(Token? previous, char token)
        {
            if (previous == null || previous.Type == TokenType.None)
            {
                switch (token)
                {
                    case '=':
                        return TokenType.Formula;
                    case '0':
                        return TokenType.Zero;
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        return TokenType.Digit;
                }
            }
            else if (previous.Type == TokenType.Digit)
            {
                switch (token)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        return TokenType.Digit;
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                        return TokenType.BinaryOperand;
                }
            }
            else if (previous.Type == TokenType.BinaryOperand)
            {
                switch (token)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        return TokenType.Digit;
                }
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
                TokenType.Zero => true,
                TokenType.Digit => true,
                TokenType.Formula => false,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }

    public class TokenValidationResult
    {
        public required TokenType ExpressionType { get; init; }
    }

    public class TokenNotValidException : Exception
    {
        public TokenNotValidException() { }
        public TokenNotValidException(string message) : base(message) { }
    }

    public class ExpressionParserException : Exception
    {
        public ExpressionParserException(int charPosition)
            : base($"Unexpected char at {charPosition}")
        {
        }
    }
}
