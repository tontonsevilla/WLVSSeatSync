using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var app = builder.Build();
app.MapDefaultEndpoints();

var failureRate = app.Configuration.GetValue("FailureRate", 0.2);  // 20% of calls fail
var charges = new ConcurrentDictionary<string, ChargeResponse>();   // idempotency key -> charge
var refunds = new ConcurrentDictionary<string, DateTimeOffset>();   // charge id -> refunded at

app.MapPost("/charges", async (ChargeRequest request, HttpRequest http, ILogger<Program> logger) =>
{
    var key = http.Headers["Idempotency-Key"].ToString();
    if (string.IsNullOrWhiteSpace(key))
        return Results.BadRequest("An Idempotency-Key header is required.");

    // Same key, same answer: a retried request returns the original charge instead of charging again.
    if (charges.TryGetValue(key, out var existing))
        return Results.Ok(existing);

    // Behave like a real provider on a bad day: slow, and sometimes down.
    await Task.Delay(Random.Shared.Next(100, 800));
    if (Random.Shared.NextDouble() < failureRate)
    {
        logger.LogWarning("Simulating an outage for key {Key}", key);
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    // A test card that always declines. Declines are not stored, so the buyer can retry with another card.
    if (request.CardToken == "tok_declined")
        return Results.Ok(new ChargeResponse($"ch_{Guid.NewGuid():N}", "declined", request.Amount, request.Currency));

    var charge = charges.GetOrAdd(key,
        _ => new ChargeResponse($"ch_{Guid.NewGuid():N}", "succeeded", request.Amount, request.Currency));

    logger.LogInformation("Charged {Amount} {Currency} as {ChargeId}", charge.Amount, charge.Currency, charge.Id);
    return Results.Ok(charge);
});

app.MapPost("/charges/{chargeId}/refund", (string chargeId, ILogger<Program> logger) =>
{
    refunds[chargeId] = DateTimeOffset.UtcNow;
    logger.LogInformation("Refunded {ChargeId}", chargeId);
    return Results.Ok();
});

app.Run();

internal sealed record ChargeRequest(decimal Amount, string Currency, string CardToken, string Reference);
internal sealed record ChargeResponse(string Id, string Status, decimal Amount, string Currency);