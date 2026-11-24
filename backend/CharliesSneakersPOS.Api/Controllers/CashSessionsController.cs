using CharliesSneakersPOS.Api.Contracts;
using CharliesSneakersPOS.Api.Filters;
using CharliesSneakersPOS.Domain.Interfaces;
using CharliesSneakersPOS.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace CharliesSneakersPOS.Api.Controllers;

[ApiController]
[Route("api/cash-sessions")]
[ServiceFilter(typeof(CashExceptionFilter))]
public sealed class CashSessionsController : ControllerBase
{
    private readonly CashSessionService _sessions;
    private readonly CashClosingService _closing;
    private readonly ICashSessionRepository _repository;

    public CashSessionsController(
        CashSessionService sessions,
        CashClosingService closing,
        ICashSessionRepository repository)
    {
        _sessions = sessions;
        _closing = closing;
        _repository = repository;
    }

    // HU-09: apertura de caja.
    [HttpPost]
    [ProducesResponseType<CashSessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashSessionResponse>> Open(
        OpenCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.OpenAsync(request.OpeningAmount, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = session.Id },
            CashSessionResponse.From(session));
    }

    // HU-10: cierre y cuadre de caja.
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType<CashClosingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashClosingResponse>> Close(
        Guid id,
        CloseCashSessionRequest request,
        CancellationToken cancellationToken)
    {
        var summary = await _closing.CloseAsync(id, request.CountedAmount, cancellationToken);

        return CashClosingResponse.From(id, summary);
    }

    [HttpGet("current")]
    [ProducesResponseType<CashSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashSessionResponse>> GetCurrent(
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetOpenAsync(cancellationToken);

        return session is null
            ? NotFound()
            : CashSessionResponse.From(session);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CashSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashSessionResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(id, cancellationToken);

        return session is null
            ? NotFound()
            : CashSessionResponse.From(session);
    }
}
