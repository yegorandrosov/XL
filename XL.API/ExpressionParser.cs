using System.Diagnostics;

namespace XL.API
{
    public interface IExpressionContext
    {
        double GetReferenceValue(string name);
    }
    
    public class ExpressionParser
    {
        private readonly IExpressionContext context;

        public ExpressionParser(IExpressionContext context)
        {
            this.context = context;
        }
        
        public Expression ParseExpression(string expression)
        {
            var tokens = ParseTokens(expression);
            var root = ConvertTokensToExpression(tokens);
            
            try
            {
                if (root.IsNumber)
                {
                    return root;
                }

                if (root.IsFormula)
                {
                    ReplaceVariablesFromContext(root);
                    return TryCalculateExpression(root.Next);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            root.IsError = true;

            return root;
        }

        private void ReplaceVariablesFromContext(Expression root)
        {
            var current = root.Next;

            do
            {
                if (!current!.IsVariable)
                {
                    current = current.Next;
                    continue;
                }
                
                var value = context.GetReferenceValue(current.StringValue);

                var newExpression = new Expression()
                {
                    Tokens = value.ToString().Select(x => new Token(x, TokenType.Digit)).ToList(),
                    Next = current.Next,
                    Previous = current.Previous,
                };

                current.Previous!.Next = newExpression;
                if (current.Next != null)
                    current.Next.Previous = newExpression;

                current = newExpression.Next;
            } while (current.Next != null);
        }

        private Expression TryCalculateExpression(Expression exp)
        {
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
                    // if (current.IsOpeningParentheses)
                    // {
                    //     TryCalculateExpression(current.Next);
                    // }
                    //
                    if (current.IsBinaryOperand)
                    {
                        opCount++;
                        if (highPrio is not { IsHighPrioOperand: true })
                        {
                            highPrio = current;
                        }   
                    }

                    current = current.Next;
                } while (current != null);
                
                // calculate high prio
                var previousArg = highPrio.Previous;
                var nextArg = highPrio.Next;

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

                newExpr.Previous = previousArg.Previous.IsOpeningParentheses ? 
                    previousArg.Previous.Previous : previousArg.Previous;
                
                previousArg.Previous.Next = newExpr;
                
                if (nextArg.Next != null)
                {
                    nextArg.Next.Previous = newExpr;
                    newExpr.Next = nextArg.Next.IsClosingParentheses ? nextArg.Next.Next : nextArg.Next;
                }

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

        public TokenType ParseTokenType(Token? previous, char token)
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

                return TokenType.Text;
            }
            else if (previous.Type == TokenType.BinaryOperand)
            {
                if (char.IsDigit(token))
                    return TokenType.Digit;
                
                if (token.IsText())
                    return TokenType.Variable;
            }
            else if (previous.Type == TokenType.FormulaSign)
            {
                if (char.IsDigit(token))
                    return TokenType.Digit;
                
                if (token.IsText())
                    return TokenType.Variable;
            }
            else if (previous.Type == TokenType.Variable)
            {
                if (char.IsDigit(token))
                    return TokenType.Variable;

                if (token.IsText())
                    return TokenType.Variable;
                
                if (token.IsBinaryOperand())
                    return TokenType.BinaryOperand;
            }
            else if (previous.Type == TokenType.Text)
            {
                return TokenType.Text;
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
    }
}
