using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var apiapp = builder.AddProject<GuidedProject_ApiApp>("apiapp");
var webapp = builder.AddProject<GuidedProject_WebApp>("webapp")
                    .WithReference(apiapp)
                    .WaitFor(apiapp);

builder.Build().Run();
