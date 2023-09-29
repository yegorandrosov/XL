namespace XL.Tests;

public class ExpressionParserTests
{
    private ExpressionParser parser;

    [SetUp]
    public void Setup()
    {
        parser = new ExpressionParser();
    }

    private char ConvertIntToChar(int number)
    {
        return (char)('0' | number);
    }


    [TestCase]
    public void NextTokenIsValid_NumericValuesInTheStartExcept0()
    {
        for (var i = 1; i < 10; i++)
        {
            var type = parser.ParseTokenType(null, ConvertIntToChar(i));

            Assert.That(type, Is.EqualTo(TokenType.Digit));
        }
    }


    [TestCase]
    public void NextTokenIsValid_0InTheStart()
    {
        var type = parser.ParseTokenType(null, ConvertIntToChar(0));

        Assert.That(type, Is.EqualTo(TokenType.Zero));
    }

    [TestCase]
    public void NextTokenIsValid_NumbersAfter0()
    {
        for (var i = 1; i < 10; i++)
        {
            var type = parser.ParseTokenType(new Token('0', TokenType.Zero), ConvertIntToChar(i));

            Assert.That(type, Is.EqualTo(TokenType.InvalidToken));
        }
    }

    [TestCase]
    public void NextTokenIsValid_NumericValuesAfterNumber()
    {
        for (var i = 0; i < 10; i++)
        {
            var type = parser.ParseTokenType(new Token('1', TokenType.Digit), ConvertIntToChar(i));

            Assert.That(type, Is.EqualTo(TokenType.Digit));
        }
    }


    private static object[] SingleDigit =
    {
        new object[] { "1", 1, },
        new object[] { "12", 12, },
        new object[] { "345", 345, },
        new object[] { "157345423", 157345423 },
    };

    [TestCaseSource(nameof(SingleDigit))]
    public void Parse_SingleNumber(string digit, int value)
    {
        var expression = parser.ParseExpression(digit);
        Assert.Multiple(() =>
        {
            Assert.That(expression.AsEnumerable().Count(), Is.EqualTo(1));
            Assert.That(expression.Value, Is.EqualTo(digit));
        });
    }

    [TestCase]
    public void Parse_Add()
    {
        var expression = parser.ParseExpression("1+1");
        var expressions = expression.AsEnumerable().ToList();
        
        Assert.That(expressions, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(expressions[0].Value, Is.EqualTo("1"));
            Assert.That(expressions[1].Value, Is.EqualTo("+"));
            Assert.That(expressions[2].Value, Is.EqualTo("1"));
        });
    }

    [TestCase]
    public void Parse_AddComplex()
    {
        var expression = parser.ParseExpression("1492+1001+467471");
        var expressions = expression.AsEnumerable().ToList();
        
        Assert.That(expressions, Has.Count.EqualTo(5));
        Assert.Multiple(() =>
        {
            Assert.That(expressions[0].Value, Is.EqualTo("1492"));
            Assert.That(expressions[1].Value, Is.EqualTo("+"));
            Assert.That(expressions[2].Value, Is.EqualTo("1001"));
            Assert.That(expressions[3].Value, Is.EqualTo("+"));
            Assert.That(expressions[4].Value, Is.EqualTo("467471"));
        });
    }

    [TestCase]
    public void Parse_MixBasicOperations()
    {
        var expression = parser.ParseExpression("432*22/321+1323-2483");
        var expressions = expression.AsEnumerable().ToList();
        
        Assert.That(expressions, Has.Count.EqualTo(9));
        Assert.Multiple(() =>
        {
            Assert.That(expressions[0].Value, Is.EqualTo("432"));
            Assert.That(expressions[1].Value, Is.EqualTo("*"));
            Assert.That(expressions[2].Value, Is.EqualTo("22"));
            Assert.That(expressions[3].Value, Is.EqualTo("/"));
            Assert.That(expressions[4].Value, Is.EqualTo("321"));
            Assert.That(expressions[5].Value, Is.EqualTo("+"));
            Assert.That(expressions[6].Value, Is.EqualTo("1323"));
            Assert.That(expressions[7].Value, Is.EqualTo("-"));
            Assert.That(expressions[8].Value, Is.EqualTo("2483"));
        });
    }
}