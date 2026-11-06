namespace WLVSSeatSync.Api.IntegrationTests;

// Every test class in this collection shares one factory, so one PostgreSQL container.
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<SeatSyncApiFactory>
{
    public const string Name = "SeatSync API";
}