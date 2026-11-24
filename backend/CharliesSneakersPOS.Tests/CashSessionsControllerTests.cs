using CharliesSneakersPOS.Api.Contracts;
using CharliesSneakersPOS.Api.Controllers;
using CharliesSneakersPOS.Api.Filters;
using CharliesSneakersPOS.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CharliesSneakersPOS.Tests;

public class CashSessionsControllerTests
{
    [Fact]
    public async Task Open_ValidRequest_ReturnsCreatedWithOpenSession()
    {
        var (controller, repository) = CreateController(netCashSales: 0m);

        var result = await controller.Open(new OpenCashSessionRequest(50000m), default);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var body = Assert.IsType<CashSessionResponse>(created.Value);
        Assert.Equal("Open", body.Status);
        Assert.Equal(50000m, body.OpeningAmount);
        Assert.Single(repository.Sessions);
    }

    [Fact]
    public async Task Close_OpenSession_ReturnsClosingSummary()
    {
        var (controller, _) = CreateController(netCashSales: 30000m);
        var created = (CreatedAtActionResult)(await controller.Open(
            new OpenCashSessionRequest(50000m), default)).Result!;
        var id = ((CashSessionResponse)created.Value!).Id;

        var result = await controller.Close(id, new CloseCashSessionRequest(79000m), default);

        var body = Assert.IsType<CashClosingResponse>(result.Value);
        Assert.Equal(id, body.CashSessionId);
        Assert.Equal(80000m, body.Expected);
        Assert.Equal(1000m, body.Shortage);
        Assert.False(body.IsBalanced);
    }

    [Fact]
    public async Task GetCurrent_NoOpenSession_ReturnsNotFound()
    {
        var (controller, _) = CreateController(netCashSales: 0m);

        var result = await controller.GetCurrent(default);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetCurrent_OpenSession_ReturnsIt()
    {
        var (controller, _) = CreateController(netCashSales: 0m);
        await controller.Open(new OpenCashSessionRequest(1000m), default);

        var result = await controller.GetCurrent(default);

        Assert.Equal("Open", result.Value!.Status);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var (controller, _) = CreateController(netCashSales: 0m);

        var result = await controller.GetById(Guid.NewGuid(), default);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ClosedSession_ReturnsClosedDetails()
    {
        var (controller, _) = CreateController(netCashSales: 0m);
        var created = (CreatedAtActionResult)(await controller.Open(
            new OpenCashSessionRequest(1000m), default)).Result!;
        var id = ((CashSessionResponse)created.Value!).Id;
        await controller.Close(id, new CloseCashSessionRequest(1000m), default);

        var result = await controller.GetById(id, default);

        Assert.Equal("Closed", result.Value!.Status);
        Assert.Equal(1000m, result.Value.CountedAmount);
        Assert.NotNull(result.Value.ClosedAt);
    }

    // Filtro de excepciones: mapeo a códigos HTTP.
    [Theory]
    [InlineData(typeof(KeyNotFoundException), 404)]
    [InlineData(typeof(ArgumentException), 400)]
    [InlineData(typeof(ArgumentOutOfRangeException), 400)]
    [InlineData(typeof(InvalidOperationException), 409)]
    [InlineData(typeof(DbUpdateConcurrencyException), 409)]
    public void Filter_MapsExceptionToStatusCode(Type exceptionType, int expectedStatus)
    {
        var context = CreateExceptionContext((Exception)Activator.CreateInstance(exceptionType, "boom")!);

        new CashExceptionFilter().OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(expectedStatus, result.StatusCode);
        Assert.True(context.ExceptionHandled);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(expectedStatus, problem.Status);
    }

    [Fact]
    public void Filter_UnknownException_IsNotHandled()
    {
        var context = CreateExceptionContext(new NotSupportedException());

        new CashExceptionFilter().OnException(context);

        Assert.Null(context.Result);
        Assert.False(context.ExceptionHandled);
    }

    private static (CashSessionsController Controller, FakeCashSessionRepository Repository) CreateController(
        decimal netCashSales)
    {
        var repository = new FakeCashSessionRepository();
        var controller = new CashSessionsController(
            new CashSessionService(repository),
            new CashClosingService(repository, new FakeCashSalesProvider(netCashSales)),
            repository);
        return (controller, repository);
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var actionContext = new ActionContext(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            new RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }
}
