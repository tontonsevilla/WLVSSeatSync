using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WLVSSeatSync.Api.Errors;

internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is ValidationException validation)
        {
            // Thrown by ValidationBehavior: the client sent bad input.
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            problem = new HttpValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            };
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The data changed while you were working on it. Reload and try again.",
            };
        }
        else
        {
            // A bug. Never send exception details to the client; they're in the logs.
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
            };
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
