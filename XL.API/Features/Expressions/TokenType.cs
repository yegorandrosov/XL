namespace XL.API.Features.Expressions;

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
}