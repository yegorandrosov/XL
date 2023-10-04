namespace XL.API.Features.Expressions;

public record Token(char Value, TokenType Type);

public enum TokenType
{
    InvalidToken = -1,
    None = 0,
    BinaryOperand,
    Digit,
    FormulaSign,
    Variable,
    Text,
    OpeningParenthesis,
    ClosingParenthesis,
    Whitespace,
}