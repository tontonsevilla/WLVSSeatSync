namespace WLVSSeatSync.Application.Abstractions;

public static class Roles
{
    public const string Attendee = "Attendee";
    public const string Organizer = "Organizer";
    public const string Staff = "Staff";
    public const string Admin = "Admin";

    public static readonly string[] All = [Attendee, Organizer, Staff, Admin];
}
