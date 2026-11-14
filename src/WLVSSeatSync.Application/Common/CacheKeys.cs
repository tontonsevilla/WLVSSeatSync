namespace WLVSSeatSync.Application.Common;

public static class CacheKeys
{
    // Tags group entries so they can be removed together.
    public const string AllEvents = "events";
    public static string Event(Guid eventId) => $"event:{eventId}";

    // Keys identify one entry.
    public static string EventDetails(Guid eventId) => $"event:{eventId}:details";
    public static string SeatMap(Guid eventId) => $"event:{eventId}:seats";

    public static string Search(string? search, DateTimeOffset? from, int page, int pageSize) =>
        $"events:search:{search?.Trim().ToLowerInvariant()}:{from:O}:{page}:{pageSize}";
}
