using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var config = builder.Configuration;

var openai = builder.AddConnectionString("openai");
// var ollama = builder.AddOllama("ollama")
//                     .WithImageTag("0.5.13")
//                     .WithDataVolume()
//                     // .WithContainerRuntimeArgs("--gpus=all")
//                     .WithOpenWebUI()
//                     .AddModel("phi4-mini");
// var hface = builder.AddOllama("hface")
//                    .WithImageTag("0.5.13")
//                    .WithDataVolume()
//                    // .WithContainerRuntimeArgs("--gpus=all")
//                    .WithOpenWebUI()
//                    .AddHuggingFaceModel("exaone", "LGAI-EXAONE/EXAONE-3.5-7.8B-Instruct-GGUF");

var apiapp = builder.AddProject<GuidedProject_ApiApp>("apiapp")
                    .WithReference(openai)
                    // .WithReference(ollama)
                    // .WithReference(hface)
                    .WithEnvironment("SemanticKernel__ServiceId", config["SemanticKernel:ServiceId"]!)
                    .WithEnvironment("GitHub__Models__ModelId", config["GitHub:Models:ModelId"]!)
                    // .WaitFor(ollama)
                    // .WaitFor(hface)
                    // Global KM settings
                    .WithEnvironment("KernelMemory__TextGeneratorType", "AzureOpenAIText")
                    .WithEnvironment("KernelMemory__DataIngestion__EmbeddingGeneratorTypes__0", "AzureOpenAIEmbedding")
                    .WithEnvironment("KernelMemory__DataIngestion__MemoryDbTypes__0", "SimpleVectorDb")
                    .WithEnvironment("KernelMemory__Retrieval__EmbeddingGeneratorType", "AzureOpenAIEmbedding")
                    .WithEnvironment("KernelMemory__Retrieval__MemoryDbType", "SimpleVectorDb")
                    // SimpleVectorDb settings
                    .WithEnvironment("KernelMemory__Services__SimpleVectorDb__StorageType", "Volatile")
                    .WithEnvironment("KernelMemory__Services__SimpleVectorDb__Directory", "_vectors")
                    // Azure OpenAI settings - Text generation
                    .WithEnvironment("KernelMemory__Services__AzureOpenAIText__Auth", "1")
                    .WithEnvironment("KernelMemory__Services__AzureOpenAIText__APIKey", config["KernelMemory:Services:AzureOpenAIText:APIKey"])
                    .WithEnvironment("KernelMemory__Services__AzureOpenAIText__Endpoint", config["KernelMemory:Services:AzureOpenAIText:Endpoint"])
                    .WithEnvironment("KernelMemory__Services__AzureOpenAIText__Deployment", config["KernelMemory:Services:AzureOpenAIText:Deployment"])
                    // Azure OpenAI settings - Embeddings
                    .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__Auth", "1")
                    .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__APIKey", config["KernelMemory:Services:AzureOpenAIEmbedding:APIKey"])
                    .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__Endpoint", config["KernelMemory:Services:AzureOpenAIEmbedding:Endpoint"])
                    .WithEnvironment("KernelMemory__Services__AzureOpenAIEmbedding__Deployment", config["KernelMemory:Services:AzureOpenAIEmbedding:Deployment"]);

var webapp = builder.AddProject<GuidedProject_WebApp>("webapp")
                    .WithReference(apiapp)
                    .WaitFor(apiapp);

builder.Build().Run();
