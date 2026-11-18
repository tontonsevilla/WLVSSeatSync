var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure (Docker containers)
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()          // keep data when you stop the app
    .WithPgAdmin();            // database UI, linked from the dashboard
var seatsyncDb = postgres.AddDatabase("seatsyncdb");

var redis = builder.AddRedis("redis");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()   // RabbitMQ web UI
    .WithDataVolume();          // keep data when you stop the app

var mongo = builder.AddMongoDB("mongo")
    .WithDataVolume()     // keep reviews when you stop the app
    .WithMongoExpress();  // a web UI for browsing documents, like pgAdmin
var docsDb = mongo.AddDatabase("docsdb");

// Your services
var payments = builder.AddProject<Projects.WLVSSeatSync_FakePayments>("payments")
    .WithEnvironment("FailureRate", "0.2");

var api = builder.AddProject<Projects.WLVSSeatSync_Api>("api")
    .WithReference(seatsyncDb).WaitFor(seatsyncDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(payments).WaitFor(payments);

api.WithReference(docsDb).WaitFor(docsDb);

// A fake SMTP server with a web inbox: catches every email, delivers none.
var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithHttpEndpoint(targetPort: 8025, name: "inbox")
    .WithEndpoint(targetPort: 1025, name: "smtp");
var smtp = mailpit.GetEndpoint("smtp");

builder.AddProject<Projects.WLVSSeatSync_Notifications>("notifications")
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WaitFor(mailpit)
    .WithEnvironment(context =>
    {
        // Aspire picks the real host and port at startup and passes them in.
        context.EnvironmentVariables["Smtp__Host"] = smtp.Property(EndpointProperty.Host);
        context.EnvironmentVariables["Smtp__Port"] = smtp.Property(EndpointProperty.Port);
    });

var web = builder.AddProject<Projects.WLVSSeatSync_Web>("web")
    .WithReference(api).WaitFor(api)
    .WithReference(redis).WaitFor(redis)
    .WithEnvironment("SeatMap__HubUrl", "/hubs/seat-map")   // new: the browser reaches the hub through the gateway
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.WLVSSeatSync_Gateway>("gateway")
    .WithReference(api).WaitFor(api)
    .WithReference(web).WaitFor(web)
    .WithExternalHttpEndpoints();

// The browser opens a SignalR connection to the API, so the API must allow the website's origin.
api.WithEnvironment("Cors__AllowedOrigins__0", web.GetEndpoint("https"));

builder.AddProject<Projects.WLVSSeatSync_Dashboard>("dashboard")
    .WithReference(api).WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();