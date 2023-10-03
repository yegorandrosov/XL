namespace XL.API.Features.Expressions;

public class ExpressionParserException : Exception
{
    public ExpressionParserException() {}
    public ExpressionParserException(int charPosition)
        : base($"Unexpected char at {charPosition}")
    { }
}