using RagPrototype.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace RagPrototype.API.Extensions;

public static class ResultExtensions
{
    public static IResult ToProblemDetails<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("No se puede convertir un resultado exitoso en ProblemDetails.");
        }

        var error = result.Error;

        var statusCode = error.Code switch
        {
            var c when c.Contains("NotFound") => StatusCodes.Status404NotFound,
            var c when c.Contains("Validation") => StatusCodes.Status400BadRequest,
            var c when c.Contains("Unauthorized") => StatusCodes.Status401Unauthorized,
            var c when c.Contains("Conflict") => StatusCodes.Status409Conflict,
            "Llm.Unavailable" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            detail: error.Description,
            statusCode: statusCode,
            title: "Error de Negocio",
            extensions: new Dictionary<string, object?>
            {
                { "error_code", error.Code }
            });
    }
}