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
var api = builder.AddProject<Projects.WLVSSeatSync_Api>("api")
    .WithReference(seatsyncDb).WaitFor(seatsyncDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbitmq).WaitFor(rabbitmq);

builder.AddProject<Projects.WLVSSeatSync_Notifications>("notifications")
    .WithReference(rabbitmq).WaitFor(rabbitmq);

builder.AddProject<Projects.WLVSSeatSync_Gateway>("gateway")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.WLVSSeatSync_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.WLVSSeatSync_Dashboard>("dashboard")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();