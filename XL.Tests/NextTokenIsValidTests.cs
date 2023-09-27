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
            var result = parser.NextTokenIsValid(new Expression(), ConvertIntToChar(i));

            Assert.IsTrue(result.Valid);
            Assert.That(result.ExpressionType, Is.EqualTo(ExpressionType.ConstantInt));
        }
    }


    [TestCase]
    public void NextTokenIsValid_0InTheStart()
    {
        var result = parser.NextTokenIsValid(new Expression(), '0');

        Assert.IsTrue(result.Valid);
        Assert.That(result.ExpressionType, Is.EqualTo(ExpressionType.Zero));
    }

    [TestCase]
    public void NextTokenIsValid_NumbersAfter0()
    {
        for (var i = 1; i < 10; i++)
        {
            var result = parser.NextTokenIsValid(new Expression()
            {
                Type = ExpressionType.Zero
            }, ConvertIntToChar(i));

            Assert.IsFalse(result.Valid);
            Assert.That(result.ExpressionType, Is.EqualTo(ExpressionType.Error));
        }
    }

    [TestCase]
    public void NextTokenIsValid_NumericValuesAfterNumber()
    {
        for (var i = 0; i < 10; i++)
        {
            var result = parser.NextTokenIsValid(new Expression()
            {
                Type = ExpressionType.ConstantInt
            }, ConvertIntToChar(i));

            Assert.IsTrue(result.Valid);
            Assert.That(result.ExpressionType, Is.EqualTo(ExpressionType.ConstantInt));
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
    public void Parse_SingleDigit(string digit, int value)
    {
        var expression = parser.Parse(digit);

        Assert.That(expression.Count(), Is.EqualTo(1));
        Assert.That(expression.Value, Is.EqualTo(digit));
    }

    [TestCase]
    public void Parse_Add()
    {
        var expression = parser.Parse("1+1");

        Assert.That(expression.Count(), Is.EqualTo(3));
        Assert.That(expression.At(0).Value, Is.EqualTo("1"));
        Assert.That(expression.At(1).Value, Is.EqualTo("+"));
        Assert.That(expression.At(2).Value, Is.EqualTo("1"));
    }
}