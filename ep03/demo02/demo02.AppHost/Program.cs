var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.demo02_ApiService>("apiservice");

//builder.AddProject<Projects.demo02_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService)
//    .WaitFor(apiService);

var consoleApp =  builder.AddProject<Projects.demo02_OpenApiPlugin>("demo02-openapiplugin")
                    .WithReference(apiService)
                    .WaitFor(apiService);

builder.Build().Run();
