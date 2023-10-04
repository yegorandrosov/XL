using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute.Extensions;
using OneOf;
using XL.API.Data;
using XL.API.Data.Cache;
using XL.API.Data.Models;
using XL.API.Features.Cells;
using XL.API.Features.Expressions;
using XL.API.Models;
using XL.Tests.Utils;

namespace XL.Tests;

[Timeout(3000)]
public class SheetIntegrationTests
{
    private UpsertSheetCell.Handler handler;
    private ApplicationDbContext applicationDbContext;

    [OneTimeSetUp]
    public void SetUp()
    {
        var mediator = Substitute.For<IMediator>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        applicationDbContext = new ApplicationDbContext(options);
        var sheetCellRepository = new SheetCellCacheRepository(applicationDbContext);
        handler = new UpsertSheetCell.Handler(applicationDbContext, mediator, sheetCellRepository);

        var parser = new ParseExpressionRequestHandler();
        var evaluator = new EvaluateExpressionRequestHandler(mediator);
        var valueProvider = new GetSheetCellValueQueryHandler(sheetCellRepository);
        
        mediator.Configure().Send(Arg.Any<ParseExpressionRequest>(), Arg.Any<CancellationToken>())
            .Returns(arg => parser.Handle(arg.Arg<ParseExpressionRequest>(), CancellationToken.None));
        
        mediator.Configure().Send(Arg.Any<EvaluateExpressionRequest>(), Arg.Any<CancellationToken>())
            .Returns(arg => evaluator.Handle(arg.Arg<EvaluateExpressionRequest>(), CancellationToken.None));
        
        mediator.Configure().Send(Arg.Any<GetSheetCellValueQuery>(), Arg.Any<CancellationToken>())
            .Returns(arg => valueProvider.Handle(arg.Arg<GetSheetCellValueQuery>(), CancellationToken.None));
    }

    private async Task<OneOf<Success<SheetCell>, Unprocessable>> UpsertCell(string cellId, string sheetId, string expression)
    {
        return await handler.Handle(new UpsertSheetCell.Command(sheetId, cellId, expression), CancellationToken.None);
    }

    [TestCase("cellA", "=1")]
    [TestCase("cellB", "1")]
    [TestCase("cellC", "Hello world!")]
    public async Task CreateOneCell_Success(string cellId, string formula)
    {
        var result = await UpsertCell(cellId, "default",  formula);
        
        Assert.IsTrue(result.IsT0);
        Assert.NotNull(result.AsT0.Value);

        var cell = applicationDbContext.SheetCells.FirstOrDefault(x => x.CellId == cellId && x.SheetId == "default");
        
        Assert.NotNull(cell);
    }
    
    [TestCase("cellA1", "=1 + ((")]
    [TestCase("cellA2", "=15 4")]
    public async Task CreateOneCell_Fail(string cellId, string formula)
    {
        var result = await UpsertCell(cellId, "default", formula);
        
        Assert.IsTrue(result.IsError());
        
        var cell = applicationDbContext.SheetCells.FirstOrDefault(x => x.CellId == cellId && x.SheetId == "default");
        
        Assert.Null(cell);
    }

    [Test]
    public async Task CreateTwoCellsReferencingOneAnother_Success()
    {
        var result1 = await UpsertCell("cell234", "default", "15");
        var result2 = await UpsertCell("cell235", "default", "=cell234+25");
            
            
        Assert.IsTrue(result1.IsT0);
        Assert.That(result1.AsT0.Value.NumericValue, Is.EqualTo(15));
        
        Assert.IsTrue(result2.IsT0);
        Assert.That(result2.AsT0.Value.NumericValue, Is.EqualTo(40));
    }

    [Test]
    public async Task CreateOneCellAndOverride_Success()
    {
        var sheetName = nameof(CreateOneCellAndOverride_Success);
        var result1 = await UpsertCell("cell234", sheetName, "15");
            
            
        Assert.IsTrue(result1.IsSuccess());
        Assert.That(result1.AsT0.Value.NumericValue, Is.EqualTo(15));
        
        var result2 = await UpsertCell("cell234", sheetName, "=25+15");
        
        Assert.IsTrue(result2.IsSuccess());
        Assert.That(result2.AsT0.Value.NumericValue, Is.EqualTo(40));
        
        var cell = applicationDbContext.SheetCells.FirstOrDefault(x => x.CellId == "cell234" && x.SheetId == sheetName);

        Assert.That(cell.NumericValue, Is.EqualTo(40));
    }
    
    
    [Test]
    public async Task CreateOneCellTwiceWithSelfReference_SuccessAndFail()
    {
        var sheetName = nameof(CreateOneCellTwiceWithSelfReference_SuccessAndFail);
        var result1 = await UpsertCell("cell1543", sheetName, "15");
        var result2 = await UpsertCell("cell1543", sheetName, "=cell1543+25");
            
        Assert.IsTrue(result1.IsT0);
        Assert.That(result1.AsT0.Value.NumericValue, Is.EqualTo(15));
        
        Assert.IsTrue(result2.IsError());
        
        var cell = applicationDbContext.SheetCells.FirstOrDefault(x => x.CellId == "cell1543" && x.SheetId == sheetName);

        Assert.That(cell.NumericValue, Is.EqualTo(15));
    }

    [Test]
    public async Task CreateThreeCellsReferencingEachOther_Success()
    {
        var sheetName = nameof(CreateThreeCellsReferencingEachOther_Success);
        var result1 = await UpsertCell("cell233", sheetName, "=143+15");
        var result2 = await UpsertCell("cell234", sheetName, "=(500-cell233)*2");
        var result3 = await UpsertCell("cell235", sheetName, "=cell234/100+234+(cell233-10)");

        Assert.IsTrue(result1.IsSuccess());
        Assert.IsTrue(result2.IsSuccess());
        Assert.IsTrue(result3.IsSuccess());
        
        var expectedResult1 = 143m + 15;
        var expectedResult2 = (500m - expectedResult1) * 2;
        var expectedResult3 = expectedResult2 / 100 + 234 + (expectedResult1 - 10);
        
        Assert.That(result1.AsT0.Value.NumericValue, Is.EqualTo(expectedResult1));
        Assert.That(result2.AsT0.Value.NumericValue, Is.EqualTo(expectedResult2));
        Assert.That(result3.AsT0.Value.NumericValue, Is.EqualTo(expectedResult3));
    }

    [Test]
    public async Task CreateTwoCellsAddNumberToText_Fail()
    {
        var sheetName = nameof(CreateTwoCellsAddNumberToText_Fail);
        var result1 = await UpsertCell("cell233", sheetName, "Hello World!");
        var result2 = await UpsertCell("cell234", sheetName, "=(500-cell233)*2");
        
        Assert.IsTrue(result1.IsSuccess());
        Assert.IsTrue(result2.IsError());
        
        var cell = applicationDbContext.SheetCells.FirstOrDefault(x => x.CellId == "cell233" && x.SheetId == sheetName);

        Assert.IsNull(cell.NumericValue);
        Assert.That(cell.Expression, Is.EqualTo("Hello World!"));
        
        cell = applicationDbContext.SheetCells.FirstOrDefault(x => x.CellId == "cell234" && x.SheetId == sheetName);

        Assert.IsNull(cell);
    }
    
    [Test]
    public async Task CreateTwoCellsAddNumberToConstantNumber_Success()
    {
        var sheetName = nameof(CreateTwoCellsAddNumberToConstantNumber_Success);
        var result1 = await UpsertCell("cell233", sheetName, "40");
        var result2 = await UpsertCell("cell234", sheetName, "=(500-cell233)*2");
        
        Assert.IsTrue(result1.IsSuccess());
        Assert.IsTrue(result2.IsSuccess());
        
        var cell = applicationDbContext.SheetCells.FirstOrDefault(x => x.CellId == "cell233" && x.SheetId == sheetName);

        Assert.That(cell.NumericValue, Is.EqualTo(40));
        
        cell = GetCellFromDb("cell234", sheetName);

        Assert.That(cell.NumericValue, Is.EqualTo(920));
    }

    private SheetCell? GetCellFromDb(string cellId, string sheetName)
    {
        return applicationDbContext.SheetCells.FirstOrDefault(x => x.CellId == cellId && x.SheetId == sheetName);
    }

    [Test]
    public async Task CreateTwoCellsAndReplaceWithText_Fail()
    {
        var sheetName = nameof(CreateTwoCellsAndReplaceWithText_Fail);
        
        var result1 = await UpsertCell("cell233", sheetName, "40");
        var result2 = await UpsertCell("cell234", sheetName, "=(500-cell233)*2");
        var result3 = await UpsertCell("cell233", sheetName, "abc");
        
        Assert.IsTrue(result1.IsSuccess());
        Assert.IsTrue(result2.IsSuccess());
        Assert.IsTrue(result3.IsError());
    }

    [Test]
    public async Task CreateThreeCellsAndReplaceMiddle_Success()
    {
        var sheetName = nameof(CreateThreeCellsAndReplaceMiddle_Success);

        var result1 = await UpsertCell("cell123", sheetName, "-15");
        Assert.IsTrue(result1.IsSuccess());
        Assert.That(result1.AsT0.Value.NumericValue, Is.EqualTo(-15));

        var result2 = await UpsertCell("cell124", sheetName, "=cell123+115");
        Assert.IsTrue(result2.IsSuccess());
        Assert.That(result2.AsT0.Value.NumericValue, Is.EqualTo(100));

        var result3 = await UpsertCell("cell125", sheetName, "=cell124+115");
        Assert.IsTrue(result3.IsSuccess());
        Assert.That(result3.AsT0.Value.NumericValue, Is.EqualTo(215));

        var result4 = await UpsertCell("cell126", sheetName, "=cell125+115");
        Assert.IsTrue(result4.IsSuccess());
        Assert.That(result4.AsT0.Value.NumericValue, Is.EqualTo(330));

        var result5 = await UpsertCell("cell125", sheetName, "=-10");
        Assert.IsTrue(result5.IsSuccess());       
        Assert.That(result5.AsT0.Value.NumericValue, Is.EqualTo(-10));

        var cell = GetCellFromDb("cell123", sheetName);
        
        Assert.That(cell.NumericValue, Is.EqualTo(-15));
        
        cell = GetCellFromDb("cell124", sheetName);
        
        Assert.That(cell.NumericValue, Is.EqualTo(100));
        
        cell = GetCellFromDb("cell125", sheetName);

        Assert.That(cell.NumericValue, Is.EqualTo(-10));

        cell = GetCellFromDb("cell126", sheetName);
        
        Assert.That(cell.NumericValue, Is.EqualTo(105));
    }
}