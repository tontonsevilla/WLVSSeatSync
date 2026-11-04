namespace WLVSSeatSync.Domain.Abstractions
{
    public abstract class Entity
    {
        // Version 7 GUIDs are time-ordered, so they index well in PostgreSQL.
        public Guid Id { get; protected init; } = Guid.CreateVersion7();
    }
}
