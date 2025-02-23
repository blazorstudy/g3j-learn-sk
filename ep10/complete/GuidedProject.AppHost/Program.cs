using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var config = builder.Configuration;

var openai = builder.AddConnectionString("openai");
var ollama = builder.AddOllama("ollama")
                    .WithDataVolume()
                    .WithOpenWebUI()
                    .AddModel("phi4");

var apiapp = builder.AddProject<GuidedProject_ApiApp>("apiapp")
                    .WithReference(openai)
                    .WithReference(ollama)
                    .WithEnvironment("SemanticKernel__ServiceId", config["SemanticKernel:ServiceId"]!)
                    .WithEnvironment("GitHub__Models__ModelId", config["GitHub:Models:ModelId"]!)
                    .WaitFor(ollama);

var webapp = builder.AddProject<GuidedProject_WebApp>("webapp")
                    .WithReference(apiapp)
                    .WaitFor(apiapp);

builder.Build().Run();
