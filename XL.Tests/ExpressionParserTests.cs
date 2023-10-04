using MediatR;
using XL.API.Features.Expressions;
using XL.API.Models;

namespace XL.Tests;

[Timeout(200)]
public class ExpressionParserTests
{    
    private IMediator mediator = null!;
    private ParseExpressionRequestHandler handler = null!;

    [SetUp]
    public void Setup()
    {
        mediator = Substitute.For<IMediator>();
        handler = new ParseExpressionRequestHandler(mediator);
    }

    private void AddVariable(string varName, double varValue)
    {
        mediator
            .Send(Arg.Is<GetSheetCellValueQuery>(o => o.CellId == varName))
            .Returns(varValue);
    }

    [TestCase("1", 1)]
    [TestCase("12", 12)]
    [TestCase("345", 345)]
    [TestCase("157345423", 157345423)]
    [TestCase("243.542", 243.542)]
    [TestCase("-23310.023", -23310.023)]
    [TestCase("-1", -1)]
    public async Task Parse_SingleNumber_Success(string input, double expected)
    {
        var result = await handler.Handle(new ParseExpressionRequest("default", input), CancellationToken.None);
        
        Assert.IsTrue(result.IsT0);
        Assert.That(result.AsT0.NumericValue, Is.EqualTo(expected));
    }

    [TestCase("=1+1", 2)]
    [TestCase("=1492+1001+467471", 469964)]
    [TestCase("=432*22/321+1323-2483", -1130.392523364486)]
    [TestCase("=43-10.5", 32.5)]
    [TestCase("=1+1+1", 3)]
    public async Task Parse_Add(string formula, double expected)
    {
        var result = await handler.Handle(new ParseExpressionRequest("default", formula), CancellationToken.None);
        
        Assert.IsTrue(result.IsT0);
        Assert.That(result.AsT0.NumericValue, Is.EqualTo(expected));
    }

    [TestCase("=a+3", "a", 2, 5)]
    [TestCase("=ab*5", "ab", 2, 10)]
    [TestCase("=var3/5", "var3", 10, 2)]
    [TestCase("=abc-5", "abc", 15, 10)]
    public async Task Parse_WithOneInt_Success(string formula, string varName, double varValue, double expected)
    {
        AddVariable(varName, varValue);
        
        var result = await handler.Handle(new ParseExpressionRequest("default", formula), CancellationToken.None);
        
        Assert.IsTrue(result.IsT0);
        Assert.IsTrue(result.AsT0.IsNumber);
        Assert.That(result.AsT0.NumericValue, Is.EqualTo(expected));
    }

    [TestCase("=X-10", "X", 11.9, 1.9)]
    public async Task Parse_WithOneDouble_Success(string formula, string varName, double varValue, double expected)
    {
        AddVariable(varName, varValue);
        
        var result = await handler.Handle(new ParseExpressionRequest("default", formula), CancellationToken.None);
        
        Assert.IsTrue(result.IsT0);
        Assert.IsTrue(result.AsT0.IsNumber);
        Assert.That(result.AsT0.NumericValue, Is.EqualTo(expected).Within(5).Ulps);
    }
    
    [TestCase("Hello world!")]
    [TestCase("45a")]
    [TestCase("carrot41")]
    [TestCase("25.00.000.0")]
    [TestCase("5+5")]
    [TestCase("-243a")]
    public async Task Parse_Text_Success(string text)
    {
        var result = await handler.Handle(new ParseExpressionRequest("default", text), CancellationToken.None);
        
        Assert.IsTrue(result.IsT0);
        Assert.IsTrue(result.AsT0.IsText);
        Assert.That(result.AsT0.StringValue, Is.EqualTo(text));
    }

    [TestCase("=(1+1)", 1+1)]
    [TestCase("=(5*4)", 5*4)]
    [TestCase("=(1-5)", 1-5)]
    [TestCase("=(1+6)*2", 7*2)]
    [TestCase("=(2+3)*(5-2)", (2+3)*(5-2))]
    [TestCase("=(10/5)-3*(4+3)", (10/5)-3*(4+3))]
    [TestCase("=10/5-3*(4+3)", (10/5)-3*(4+3))]
    [TestCase("=(12+(5+(4+3)))", (12+(5+(4+3))))]
    [TestCase("=(12+(5+(4+3)))*23", (12+(5+(4+3)))*23)]
    [TestCase("=(12+(5+(4+3)))*(23-5)", (12+(5+(4+3)))*(23-5))]
    [TestCase("=(-12+243+15)", (-12+243+15))]
    [TestCase("=(-12+(-243)+15)", (-12+(-243)+15))]
    public async Task Parse_ParenthesisSimple_Success(string formula, double expected)
    {
        var expression = await handler.Handle(new ParseExpressionRequest("default", formula), CancellationToken.None);
        
        Assert.IsTrue(expression.IsT0);
        Assert.IsTrue(expression.AsT0.IsNumber);
        Assert.That(expression.AsT0.NumericValue, Is.EqualTo(expected));
    }

    [Test]
    public async Task Parse_FormulaWithManyVars_Success()
    {
        AddVariable("abc", 5);
        AddVariable("A3", 4);
        AddVariable("text", 3);
        
        var result = await handler.Handle(new ParseExpressionRequest("default", "=abc+A3+text"), CancellationToken.None);

        Assert.IsTrue(result.IsT0);
        Assert.IsTrue(result.AsT0.IsNumber);
        Assert.That(result.AsT0.NumericValue, Is.EqualTo(5 + 4 + 3));
        CollectionAssert.AreEquivalent(new[] { "abc", "A3", "text" }, result.AsT0.DependentVariables);
    }

    [Test]
    public async Task Parse_FormulaMix_Success()
    {
        AddVariable("abc", 5);
        AddVariable("A3", 10);
        AddVariable("text", 60);

        var result = await handler.Handle(new ParseExpressionRequest("default", "=(abc/A3)+(text/(5*2))"), CancellationToken.None);

        Assert.IsTrue(result.IsT0);
        Assert.IsTrue(result.AsT0.IsNumber);
        Assert.That(result.AsT0.NumericValue, Is.EqualTo(0.5+6).Within(5).Ulps);
        CollectionAssert.AreEquivalent(result.AsT0.DependentVariables, new [] { "abc", "A3", "text" });
    }
    
    
    [Test]
    public async Task Parse_SameVariableMultipleTimes_Success()
    {
        AddVariable("abc", 5);

        var result = await handler.Handle(new ParseExpressionRequest("default", "=abc*abc*abc-abc"), CancellationToken.None);
        
        Assert.IsTrue(result.IsT0);
        Assert.IsTrue(result.AsT0.IsNumber);
        Assert.That(result.AsT0.NumericValue, Is.EqualTo(5*5*5-5));
        CollectionAssert.AreEquivalent(result.AsT0.DependentVariables, new [] { "abc" });
    }

    [TestCase("=--")]
    [TestCase("=-(")]
    [TestCase("=-001*(")]
    [TestCase("=(()")]
    [TestCase("=++()123AA")]
    public async Task Parse_InvalidSyntax_Error(string formula)
    {
        var result = await handler.Handle(new ParseExpressionRequest("default", formula), CancellationToken.None);
        
        Assert.IsTrue(result.IsT1);
        Assert.NotNull(result.AsT1);
    }

    [TestCase("=a+5")]
    [TestCase("=(5+3)*c")]
    [TestCase("=33/abc")]
    [TestCase("=abc 33 - 5")]
    public async Task Parse_UnknownVariable_Error(string formula)
    {
        mediator
            .Send(Arg.Any<GetSheetCellValueQuery>())
            .Returns(new NotFound());

        var result = await handler.Handle(new ParseExpressionRequest("default", formula), CancellationToken.None);
        
        Assert.IsTrue(result.IsT1);
        Assert.NotNull(result.AsT1);
    }


    [TestCase("=123 + 456 - 789", 123 + 456 - 789)]
    [TestCase("=(123 + (456 - 789 )", (123 + (456 - 789 )))]
    [TestCase("=137637*2   +13", 137637*2   +13)]
    public async Task Parse_FormulaWithWhiteSpace(string formula, double expected)
    {
        var result = await handler.Handle(new ParseExpressionRequest("default", formula), CancellationToken.None);
        
        Assert.IsTrue(result.IsT0);
        Assert.That(result.AsT0.NumericValue, Is.EqualTo(expected));
    }

    [Test]
    public async Task Parse_FormulaWithWhitespaceAndVariables()
    {
        AddVariable("x", 384);

        string formula = "= x * 2 - x / 2";
        
        var result = await handler.Handle(new ParseExpressionRequest("default", formula), CancellationToken.None);

        Assert.IsTrue(result.IsT0);
        Assert.That(result.AsT0.NumericValue, Is.EqualTo(384*2 - 384/2));
    }
}