using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Api.Errors;
using WLVSSeatSync.Application.Venues;

namespace WLVSSeatSync.Api.Endpoints;

public static class VenueEndpoints
{
    public static IEndpointRouteBuilder MapVenueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/venues").WithTags("Venues");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("/", CreateAsync).RequireAuthorization(Policies.ManageVenues);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<VenueSummary>>> GetAllAsync(
        ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetVenuesQuery(), cancellationToken));

    private static async Task<Results<Ok<VenueDetails>, ProblemHttpResult>> GetByIdAsync(
        Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetVenueByIdQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Created<CreatedResponse>, ProblemHttpResult>> CreateAsync(
        CreateVenueCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/venues/{result.Value}", new CreatedResponse(result.Value))
            : result.ToProblem();
    }
}
