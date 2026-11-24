using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace CharliesSneakersPOS.Api.Filters;

// Traduce las excepciones del módulo de caja a respuestas HTTP (ProblemDetails).
public sealed class CashExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (status, title) = context.Exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Cash session not found"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            // Caja ya abierta/cerrada o cierre concurrente (RowVersion).
            InvalidOperationException or DbUpdateConcurrencyException
                => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (0, string.Empty)
        };

        if (status == 0)
            return;

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = context.Exception is DbUpdateConcurrencyException
                ? "The cash session was modified by another request."
                : context.Exception.Message
        })
        { StatusCode = status };

        context.ExceptionHandled = true;
    }
}
