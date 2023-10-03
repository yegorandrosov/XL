namespace XL.API;

public enum TokenType
{
    InvalidToken = -1,
    None = 0,
    BinaryOperand,
    Digit,
    FormulaSign,
    Variable,
    Text
}