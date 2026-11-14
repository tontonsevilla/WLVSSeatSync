var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure (Docker containers)
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()          // keep data when you stop the app
    .WithPgAdmin();            // database UI, linked from the dashboard
var seatsyncDb = postgres.AddDatabase("seatsyncdb");

var redis = builder.AddRedis("redis");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();   // RabbitMQ web UI

// Your services
var payments = builder.AddProject<Projects.WLVSSeatSync_FakePayments>("payments")
    .WithEnvironment("FailureRate", "0.2");

var api = builder.AddProject<Projects.WLVSSeatSync_Api>("api")
    .WithReference(seatsyncDb).WaitFor(seatsyncDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(payments).WaitFor(payments);

builder.AddProject<Projects.WLVSSeatSync_Notifications>("notifications")
    .WithReference(rabbitmq).WaitFor(rabbitmq);

builder.AddProject<Projects.WLVSSeatSync_Gateway>("gateway")
    .WithReference(api)
    .WithExternalHttpEndpoints();

var web = builder.AddProject<Projects.WLVSSeatSync_Web>("web")
    .WithReference(api)
    .WithReference(redis).WaitFor(redis)
    .WithExternalHttpEndpoints();

// The browser opens a SignalR connection to the API, so the API must allow the website's origin.
api.WithEnvironment("Cors__AllowedOrigins__0", web.GetEndpoint("https"));

builder.AddProject<Projects.WLVSSeatSync_Dashboard>("dashboard")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();