using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;

using WLVSSeatSync.Api.Auth;
using WLVSSeatSync.Api.Errors;
using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Application.Analytics;
using WLVSSeatSync.Application.Audit;
using WLVSSeatSync.Application.Events;

namespace WLVSSeatSync.Api.Endpoints;

// Endpoints that exist for the organizer and admin dashboard.
public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var manage = app.MapGroup("/manage").WithTags("Dashboard").RequireAuthorization(Policies.ManageEvents);
        manage.MapGet("/events", GetEventsAsync);
        manage.MapGet("/events/{id:guid}/sales", GetSalesAsync);

        var admin = app.MapGroup("/admin").WithTags("Dashboard").RequireAuthorization(Policies.Administer);
        admin.MapGet("/audit", GetAuditAsync);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ManagedEvent>>> GetEventsAsync(
        ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetManagedEventsQuery(), cancellationToken));

    private static async Task<Results<Ok<EventSalesReport>, ProblemHttpResult>> GetSalesAsync(
    Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventSalesQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyList<AuditEntry>>> GetAuditAsync(
    ISender sender, CancellationToken cancellationToken, string? action = null, int take = 100) =>
    TypedResults.Ok(await sender.Send(new GetAuditLogQuery(action, take), cancellationToken));
}
