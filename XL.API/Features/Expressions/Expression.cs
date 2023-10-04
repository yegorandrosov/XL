﻿using System.Text;

namespace XL.API.Features.Expressions;

public class Expression
{
    public Expression? Previous { get; set; }
    public Expression? Next { get; set; }
    public List<Token> Tokens { get; init; } = new();
    public HashSet<string> DependentVariables { get; set; } = new();
    public double NumericValue => IsFormula ? Next.NumericValue : double.Parse(string.Join("", Tokens.Select(x => x.Value)));
    public bool IsText => Tokens.Any(x => x.Type == TokenType.Text);
    public bool IsWhitespace => Tokens.Any(x => x.Type == TokenType.Whitespace);
    public bool IsFormula => Tokens[0].Value == '=';
    public bool IsNumber => Tokens.All(x => x.Type == TokenType.Digit);
    public bool IsVariable => Tokens.All(x => x.Type == TokenType.Variable);
    public bool IsOpeningParentheses => Tokens[0].Value == '(';
    public bool IsClosingParentheses => Tokens[0].Value == ')';
    public bool IsBinaryOperand => Tokens[0].Type == TokenType.BinaryOperand;
    public bool IsHighPrioOperand => Tokens[0].Value switch
    {
        '*' => true,
        '/' => true,
        '-' => false,
        '+' => false,
        _ => throw new ArgumentOutOfRangeException()
    };
    
    public bool IsError { get; set; }
    
    public string StringValue => StringBuilderValue.ToString();

    public StringBuilder StringBuilderValue
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var c in Tokens.Select(x => x.Value))
            {
                sb.Append(c);
            }

            return sb;
        }
    }

    public override string ToString()
    {
        var current = this;
        var sb = new StringBuilder();
        do
        {
            sb.Append(current.StringBuilderValue);
            current = current.Next;
        } while (current != null);
        
        return sb.ToString();
    }
}