using Microsoft.AspNetCore.Http.HttpResults;

using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Api.Errors;

public static class ResultExtensions
{
    public static ProblemHttpResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("A successful result has no problem to report.");

        var status = result.Error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        return TypedResults.Problem(statusCode: status, title: result.Error.Code, detail: result.Error.Message);
    }
}
