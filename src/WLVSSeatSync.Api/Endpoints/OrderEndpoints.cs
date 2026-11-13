using System.Net;
using System.Security.Claims;

using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Api.Errors;
using WLVSSeatSync.Application.Orders;
using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Api.Endpoints;

// What the client sends. No customer ID: that always comes from the token.
public sealed record PlaceOrderRequest(Guid EventId, IReadOnlyList<Guid> EventSeatIds, string? PromoCode = null);
public sealed record PayOrderRequest(string CardToken);
public sealed record CancelOrderRequest(Guid orderId);

public static class OrderEndpoints
{
    public const string RateLimitPolicy = "orders";

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders").RequireAuthorization();

        group.MapPost("/", PlaceAsync).RequireRateLimiting(RateLimitPolicy);
        group.MapPost("/{id:guid}/pay", PayAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
        group.MapGet("/mine", GetMineAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);

        return app;
    }

    private static async Task<Results<Created<PlacedOrder>, ProblemHttpResult>> PlaceAsync(
        PlaceOrderRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        var command = new PlaceOrderCommand(user.GetUserId(), request.EventId, request.EventSeatIds, request.PromoCode);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"/orders/{result.Value.OrderId}", result.Value)
            : result.ToProblem();
    }

    private static async Task<Results<Ok<PaidOrder>, ProblemHttpResult>> PayAsync(
        Guid id, PayOrderRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PayOrderCommand(user.GetUserId(), id, request.CardToken), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteOrderCommand(user.GetUserId(), id), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyList<OrderSummary>>> GetMineAsync(
        ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetMyOrdersQuery(user.GetUserId()), cancellationToken));

    private static async Task<Results<Ok<OrderDetails>, ProblemHttpResult>> GetByIdAsync(
        Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderByIdQuery(user.GetUserId(), id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}
