namespace XL.API.Features.Expressions;

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

    public static bool IsFormulaSign(this char ch) => ch == '=';

    public static bool IsText(this char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    public static bool IsClosingParenthesis(this char c) => c is ')';

    public static bool IsOpeningParenthesis(this char c) => c is '(';

    public static bool IsMinus(this char c) => c is '-';

    public static bool IsWhitespace(this char c) => c is ' ';
}