using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Api.Errors;
using WLVSSeatSync.Application.Common;
using WLVSSeatSync.Application.Events;

namespace WLVSSeatSync.Api.Endpoints;

public sealed record CreatedResponse(Guid Id);

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events").WithTags("Events");

        group.MapGet("/", SearchAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/{id:guid}/seats", GetSeatMapAsync);
        group.MapPost("/", CreateAsync).RequireAuthorization(Policies.ManageEvents);
        group.MapPost("/{id:guid}/publish", PublishAsync).RequireAuthorization(Policies.ManageEvents);

        return app;
    }

    private static async Task<Ok<PagedList<EventSummary>>> SearchAsync(
        ISender sender, CancellationToken cancellationToken,
        string? search = null, DateTimeOffset? from = null, int page = 1, int pageSize = 20) =>
        TypedResults.Ok(await sender.Send(new SearchEventsQuery(search, from, page, pageSize), cancellationToken));

    private static async Task<Results<Ok<EventDetails>, ProblemHttpResult>> GetByIdAsync(
        Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventByIdQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyList<SeatMapSeat>>, ProblemHttpResult>> GetSeatMapAsync(
        Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSeatMapQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Created<CreatedResponse>, ProblemHttpResult>> CreateAsync(
        CreateEventCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/events/{result.Value}", new CreatedResponse(result.Value))
            : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> PublishAsync(
        Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishEventCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
