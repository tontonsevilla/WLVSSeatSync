using System.Security.Claims;

using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Api.Errors;
using WLVSSeatSync.Application.Reviews;

namespace WLVSSeatSync.Api.Endpoints;

public sealed record AddReviewRequest(int Rating, string Comment);

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events/{id:guid}/reviews").WithTags("Reviews");

        group.MapGet("/", GetAsync);
        group.MapPost("/", AddAsync).RequireAuthorization();

        return app;
    }

    private static async Task<Ok<EventReviews>> GetAsync(Guid id, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetEventReviewsQuery(id), cancellationToken));

    private static async Task<Results<NoContent, ProblemHttpResult>> AddAsync(
        Guid id, AddReviewRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        var command = new AddReviewCommand(id, user.GetUserId(), request.Rating, request.Comment);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
