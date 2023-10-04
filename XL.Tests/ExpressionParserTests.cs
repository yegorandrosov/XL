using MediatR;
using XL.API.Features.Expressions;
using XL.API.Models;

namespace XL.Tests;

[Timeout(200)]
public class ExpressionParserTests
{    
    private IMediator mediator = null!;
    private ParseExpressionRequestHandler parser = null!;
    private EvaluateExpressionRequestHandler evaluator = null!;

    [SetUp]
    public void Setup()
    {
        mediator = Substitute.For<IMediator>();
        parser = new ParseExpressionRequestHandler();
        evaluator = new EvaluateExpressionRequestHandler(mediator);
    }

    private void AddVariable(string varName, decimal varValue)
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
    public async Task Parse_SingleNumber_Success(string input, decimal expected)
    {
        var expression = await parser.Handle(new ParseExpressionRequest(input), CancellationToken.None);
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);
        
        Assert.IsTrue(result.IsSuccess);
        Assert.That(result.NumericValue, Is.EqualTo(expected));
    }

    [TestCase("=1+1", 2)]
    [TestCase("=1492+1001+467471", 469964)]
    [TestCase("=432*22/321+1323-2483", 432*22/321+1323-2483)]
    [TestCase("=43-10.5", 32.5)]
    [TestCase("=1+1+1", 3)]
    public async Task Parse_Add(string formula, decimal expected)
    {
        var expression = await parser.Handle(new ParseExpressionRequest(formula), CancellationToken.None);
        
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);
        
        Assert.IsTrue(result.IsSuccess);
        Assert.That(result.NumericValue, Is.EqualTo(expected));
    }

    [TestCase("=a+3", "a", 2, 5)]
    [TestCase("=ab*5", "ab", 2, 10)]
    [TestCase("=var3/5", "var3", 10, 2)]
    [TestCase("=abc-5", "abc", 15, 10)]
    public async Task Parse_WithOneInt_Success(string formula, string varName, decimal varValue, decimal expected)
    {
        AddVariable(varName, varValue);
        
        
        var expression = await parser.Handle(new ParseExpressionRequest(formula), CancellationToken.None);
        
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.IsNumber);
        Assert.That(result.NumericValue, Is.EqualTo(expected));
    }

    [TestCase("=X-10", "X", 11.9, 1.9)]
    public async Task Parse_WithOnedecimal_Success(string formula, string varName, decimal varValue, decimal expected)
    {
        AddVariable(varName, varValue);
        var expression = await parser.Handle(new ParseExpressionRequest(formula), CancellationToken.None);
        
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);

        Assert.IsTrue(result.IsNumber);
        Assert.That(result.NumericValue, Is.EqualTo(expected));
    }
    
    [TestCase("Hello world!")]
    [TestCase("45a")]
    [TestCase("carrot41")]
    [TestCase("25.00.000.0")]
    [TestCase("5+5")]
    [TestCase("-243a")]
    public async Task Parse_Text_Success(string text)
    {
        var expression = await parser.Handle(new ParseExpressionRequest(text), CancellationToken.None);
        
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.IsText);
        Assert.That(result.StringValue, Is.EqualTo(text));
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
    public async Task Parse_ParenthesisSimple_Success(string formula, decimal expected)
    {
        var expression = await parser.Handle(new ParseExpressionRequest(formula), CancellationToken.None);
        
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.IsNumber);
        Assert.That(result.NumericValue, Is.EqualTo(expected));
    }

    [Test]
    public async Task Parse_FormulaWithManyVars_Success()
    {
        AddVariable("abc", 5);
        AddVariable("A3", 4);
        AddVariable("text", 3);
        var expression = await parser.Handle(new ParseExpressionRequest("=abc+A3+text"), CancellationToken.None);
        
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.IsNumber);
        Assert.That(result.NumericValue, Is.EqualTo(5 + 4 + 3));
        CollectionAssert.AreEquivalent(new[] { "abc", "A3", "text" }, result.DependentVariables);
    }

    [Test]
    public async Task Parse_FormulaMix_Success()
    {
        AddVariable("abc", 5);
        AddVariable("A3", 10);
        AddVariable("text", 60);

        var expression = await parser.Handle(new ParseExpressionRequest("=(abc/A3)+(text/(5*2))"), CancellationToken.None);
        
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.IsNumber);
        Assert.That(result.NumericValue, Is.EqualTo(0.5+6).Within(5).Ulps);
        CollectionAssert.AreEquivalent(result.DependentVariables, new [] { "abc", "A3", "text" });
    }
    
    
    [Test]
    public async Task Parse_SameVariableMultipleTimes_Success()
    {
        AddVariable("abc", 5);

        var expression = await parser.Handle(new ParseExpressionRequest("=abc*abc*abc-abc"), CancellationToken.None);
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);
        
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.IsNumber);
        Assert.That(result.NumericValue, Is.EqualTo(5*5*5-5));
        CollectionAssert.AreEquivalent(result.DependentVariables, new [] { "abc" });
    }

    [TestCase("=--")]
    [TestCase("=-(")]
    [TestCase("=-001*(")]
    [TestCase("=(()")]
    [TestCase("=++()123AA")]
    public async Task Parse_InvalidSyntax_Error(string formula)
    {
        var expression = await parser.Handle(new ParseExpressionRequest(formula), CancellationToken.None);
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);

        Assert.IsTrue(result.IsError);
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

        var expression = await parser.Handle(new ParseExpressionRequest(formula), CancellationToken.None);
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);

        Assert.IsTrue(result.IsError);
    }


    [TestCase("=123 + 456 - 789", 123 + 456 - 789)]
    [TestCase("=(123 + (456 - 789 )", (123 + (456 - 789 )))]
    [TestCase("=137637*2   +13", 137637*2   +13)]
    public async Task Parse_FormulaWithWhiteSpace_Success(string formula, decimal expected)
    {
        var expression = await parser.Handle(new ParseExpressionRequest(formula), CancellationToken.None);
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.That(result.NumericValue, Is.EqualTo(expected));
    }

    [Test]
    public async Task Parse_FormulaWithWhitespaceAndVariables_Success()
    {
        AddVariable("x", 384);

        string formula = "= x * 2 - x / 2";
        var expression = await parser.Handle(new ParseExpressionRequest(formula), CancellationToken.None);
        var result = await evaluator.Handle(new EvaluateExpressionRequest("default", expression), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.That(result.NumericValue, Is.EqualTo(384*2 - 384/2));
    }
}