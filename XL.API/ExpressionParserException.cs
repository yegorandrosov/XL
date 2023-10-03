namespace XL.API;

public class ExpressionParserException : Exception
{
    public ExpressionParserException(int charPosition)
        : base($"Unexpected char at {charPosition}")
    {
    }
}