namespace WLVSSeatSync.Api.Auth;

public static class Policies
{
    public const string ManageEvents = nameof(ManageEvents); // Organizer or Admin
    public const string ManageVenues = nameof(ManageVenues); // Admin only
    public const string Administer = nameof(Administer); // Admin only: audit log, background jobs
}
