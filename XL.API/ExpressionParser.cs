using System.Text;

namespace XL.API
{
    public enum ExpressionType
    {
        None = 0,
        Error,
        Zero,
        BinaryOperand,
        ConstantInt,
        ConstantDouble,
        Formula
    }

    public class Expression
    {
        public Expression? Previous { get; set; }
        public Expression? Next { get; set; }

        private StringBuilder stringBuilder = new StringBuilder();

        public Expression() { }
        public Expression(char initialValue) { stringBuilder.Append(initialValue); }

        public void Append(char ch)
        {
            stringBuilder.Append(ch);
        }

        public void Complete()
        {
            Value = stringBuilder.ToString();
        }

        public int Count()
        {
            var current = this;
            var counter = 0;
            do
            {
                current = current.Next;
                counter++;
            } while (current != null);

            return counter;
        }

        public Expression At(int position)
        {
            var current = this;
            var counter = 0;

            while (current != null)
            {
                if (counter == position)
                    return current;

                current = current.Next;
                counter++;
            }

            throw new ArgumentOutOfRangeException(nameof(position));
        }

        public string? Value { get; private set; }
        public ExpressionType Type { get; set; }
    }

    public class ExpressionParser
    {
        public Expression Parse(string expression)
        {
            var rootExpression = new Expression() { Type = ExpressionType.None };
            var currentExpression = rootExpression;

            for (var i = 0; i < expression.Length; i++)
            {
                var c = expression[i];

                var tokenValidationResult = NextTokenIsValid(currentExpression.Previous, c);

                if (!tokenValidationResult.Valid)
                {
                    throw new ExpressionParserException(expression, i);
                }

                if (!tokenValidationResult.PreviousExpressionEnded)
                {
                    currentExpression.Append(c);
                    currentExpression.Type = tokenValidationResult.ExpressionType;
                }

                var isLastElement = i == expression.Length - 1;
                if (tokenValidationResult.PreviousExpressionEnded || isLastElement)
                {
                    currentExpression.Complete();

                    if (!isLastElement)
                    {
                        var nextExpression = new Expression(c)
                        {
                            Type = tokenValidationResult.ExpressionType,
                        };
                        currentExpression.Next = nextExpression;
                        nextExpression.Previous = currentExpression;

                        currentExpression = nextExpression;
                    }
                }
            }

            return rootExpression;
        }

        public TokenValidationResult NextTokenIsValid(Expression? previous, char token)
        {
            if (previous == null || previous.Type == ExpressionType.None)
            {
                switch (token)
                {
                    case '=':
                        return new TokenValidationResult()
                        {
                            ExpressionType = ExpressionType.Formula,
                            PreviousExpressionEnded = false,
                            Valid = true,

                        };
                    case '0':
                        return new TokenValidationResult()
                        {
                            ExpressionType = ExpressionType.Zero,
                            PreviousExpressionEnded = false,
                            Valid = true,
                        };
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        return new TokenValidationResult()
                        {
                            ExpressionType = ExpressionType.ConstantInt,
                            PreviousExpressionEnded = false,
                            Valid = true,
                        };
                }
            }
            else if (previous.Type == ExpressionType.Zero)
            {
                switch (token)
                {
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                        return new TokenValidationResult()
                        {
                            ExpressionType = ExpressionType.BinaryOperand,
                            PreviousExpressionEnded = true,
                            Valid = true,
                        };
                }
            }
            else if (previous.Type == ExpressionType.ConstantInt)
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
                        return new TokenValidationResult()
                        {
                            ExpressionType = ExpressionType.ConstantInt,
                            PreviousExpressionEnded = false,
                            Valid = true,
                        };
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                        return new TokenValidationResult()
                        {
                            ExpressionType = ExpressionType.BinaryOperand,
                            PreviousExpressionEnded = true,
                            Valid = true,
                        };
                }
            }
            else if (previous.Type == ExpressionType.BinaryOperand)
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
                        return new TokenValidationResult()
                        {
                            ExpressionType = ExpressionType.ConstantInt,
                            PreviousExpressionEnded = true,
                            Valid = true,
                        };
                }
            }
            return new TokenValidationResult()
            {
                ExpressionType = ExpressionType.Error,
                PreviousExpressionEnded = true,
                Valid = false,
            };
        }
    }

    public class TokenValidationResult
    {
        public required bool Valid { get; init; }
        public required ExpressionType ExpressionType { get; init; }
        public required bool PreviousExpressionEnded { get; init; }

        public bool IsNumber => ExpressionType == ExpressionType.Zero || ExpressionType == ExpressionType.ConstantInt;
    }


    public class ExpressionParserException : Exception
    {
        public ExpressionParserException(string expression, int charPosition)
            : base($"Unexpected char at {charPosition}")
        {
            Expression = expression;
        }

        public string Expression { get; }
    }

    public static class ExpressionTypeExtensions
    {
        public static bool IsInt(this ExpressionType type) => type == ExpressionType.Zero || type == ExpressionType.ConstantInt;
    }
}
