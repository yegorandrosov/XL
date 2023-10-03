namespace XL.Tests;

[Timeout(1000)]
public class ExpressionParserTests
{
    private ExpressionParser parser = null!;
    private IExpressionContext context = null!;

    [SetUp]
    public void Setup()
    {
        context = Substitute.For<IExpressionContext>();
        parser = new ExpressionParser(context);
    }

    [TestCase]
    public void NextTokenIsValid_NumericValuesInTheStartExcept0()
    {
        for (var i = 1; i < 10; i++)
        {
            var type = parser.ParseTokenType(null, Utils.ConvertIntToChar(i));

            Assert.That(type, Is.EqualTo(TokenType.Digit));
        }
    }


    [TestCase]
    public void NextTokenIsValid_0InTheStart()
    {
        for (var i = 0; i < 10; i++)
        {
            var type = parser.ParseTokenType(null, Utils.ConvertIntToChar(i));

            Assert.That(type, Is.EqualTo(TokenType.Digit));
        }
    }

    [TestCase]
    public void NextTokenIsValid_NumericValuesAfterNumber()
    {
        for (var i = 0; i < 10; i++)
        {
            var type = parser.ParseTokenType(new Token('1', TokenType.Digit), Utils.ConvertIntToChar(i));

            Assert.That(type, Is.EqualTo(TokenType.Digit));
        }
    }

    private static object[] SingleDigit =
    {
        new object[] { "1", 1, },
        new object[] { "12", 12, },
        new object[] { "345", 345, },
        new object[] { "157345423", 157345423 },
        new object[] { "243.542", 243.542 },
    };


    [TestCaseSource(nameof(SingleDigit))]
    public void Parse_SingleNumber(string digit, double value)
    {
        var expression = parser.ParseExpression(digit);
        Assert.Multiple(() =>
        {
            Assert.That(expression.AsEnumerable().Count(), Is.EqualTo(1));
            Assert.That(expression.NumericValue, Is.EqualTo(value));
        });
    }

    [TestCase("=1+1", 2)]
    [TestCase("=1492+1001+467471", 469964)]
    [TestCase("=432*22/321+1323-2483", -1130.392523364486)]
    [TestCase("=43-10.5", 32.5)]
    public void Parse_Add(string formula, double result)
    {
        var expression = parser.ParseExpression(formula);
        
        Assert.Multiple(() =>
        {
            Assert.That(expression.ToString(), Is.EqualTo(result.ToString()));
            Assert.That(expression.NumericValue, Is.EqualTo(result).Within(5).Ulps);
        });
        
        Assert.Pass("{0} --> {1}", formula, expression.NumericValue);
    }

    [TestCase("=a+3", "a", 2, 5)]
    [TestCase("=ab*5", "ab", 2, 10)]
    [TestCase("=var3/5", "var3", 10, 2)]
    [TestCase("=abc-5", "abc", 15, 10)]
    [TestCase("=X-10", "X", 11.9, 1.9)]
    public void Parse_WithOneVariable(string formula, string varName, double varValue, double result)
    {
        context.GetReferenceValue(varName).Returns(varValue);

        var expression = parser.ParseExpression(formula);
        
        Assert.That(expression.NumericValue, Is.EqualTo(result).Within(5).Ulps);
        
        Assert.Pass("{0} with {2}={3} --> {1}", formula, expression.NumericValue, varName, varValue);
    }

    [TestCase("Hello world!")]
    [TestCase("45a")]
    [TestCase("carrot41")]
    [TestCase("25.00.000.0")]
    [TestCase("5+5")]
    public void Parse_Text(string text)
    {
        var expression = parser.ParseExpression(text);
        
        Assert.That(expression.StringValue, Is.EqualTo(text));
    }
}