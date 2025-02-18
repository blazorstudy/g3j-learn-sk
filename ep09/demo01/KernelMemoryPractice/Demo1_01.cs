using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Xunit.Abstractions;

namespace KernelMemoryPractice
{
    public class Demo1_01
    {
        ITestOutputHelper output;

        public Demo1_01(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task AskToText()
        {
            var index = Guid.NewGuid().ToString().Replace("-", "");
            var memory = KernelMemoryGenerator.GetMemory();

            var docId = await memory.ImportTextAsync(@"물리학에서 질량-에너지 등가성은 시스템의 정지 상태에서 질량과 에너지 사이의 관계를 말하며, 
이 두 양은 곱셈 상수와 측정 단위에 의해서만 차이가 납니다. 
이 원리는 물리학자 알베르트 아인슈타인의 공식인 E = m*c^2로 설명됩니다.", index: index);

            var question = "E = m*c^2?가 뭐야?";
            await Answer(question, memory, index);
        }

        [Fact]
        public async Task AskToPdf()
        {
            var index = Guid.NewGuid().ToString().Replace("-", "");
            var memory = KernelMemoryGenerator.GetMemory();

            var docId = await memory.ImportDocumentAsync("file4-KM-Readme.pdf", documentId: index, index: index);
            var question = "Kernel Memory가 뭐야?";

            await Answer(question, memory, index);
        }

        [Fact]
        public async Task AskToExcel()
        {
            var index = Guid.NewGuid().ToString().Replace("-", "");
            var memory = KernelMemoryGenerator.GetMemory();

            var docId = await memory.ImportDocumentAsync("file8-data.xlsx", documentId: index, index: index);
            var question = "어떤 나라가 공식적으로 긴 이름(정식 국가명)을 설정하지 않았니?";

            await Answer(question, memory, index);
        }

        [Fact]
        public async Task Search()
        {
            var index = Guid.NewGuid().ToString().Replace("-", "");
            var memory = KernelMemoryGenerator.GetMemory();

            var docId = await memory.ImportDocumentAsync("file4-KM-Readme.pdf", documentId: index, index: index);
            var question = "Kernel Memory가 뭐야?";

            var answer = await memory.SearchAsync(question, index: index);

            output.WriteLine(JsonSerializer.Serialize(answer, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
        }

        [Fact]
        public async Task Search_Limit()
        {
            var index = Guid.NewGuid().ToString().Replace("-", "");
            var memory = KernelMemoryGenerator.GetMemory();

            var docId = await memory.ImportDocumentAsync("file4-KM-Readme.pdf", documentId: index, index: index);
            var question = "Kernel Memory가 뭐야?";

            var answer = await memory.SearchAsync(question, index: index, limit: 1);

            output.WriteLine(JsonSerializer.Serialize(answer, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
        }

        [Fact]
        public async Task Search_Relevance()
        {
            var index = Guid.NewGuid().ToString().Replace("-", "");
            var memory = KernelMemoryGenerator.GetMemory();

            var docId = await memory.ImportDocumentAsync("file4-KM-Readme.pdf", documentId: index, index: index);
            var question = "Kernel Memory가 뭐야?";

            var answer = await memory.SearchAsync(question, index: index, minRelevance: 0.3);

            output.WriteLine(JsonSerializer.Serialize(answer, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
        }


        [Fact]
        public async Task SemanticKernel_Plugin()
        {
            var kernel = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
                    deploymentName: Configuration.TextGenerationDeploymentName,
                    endpoint: Configuration.AzureOpenAIEndpoint,
                    apiKey: Configuration.AzureOpenAIApiKey)
                .Build();

            var memory = KernelMemoryGenerator.GetMemory();

            var docId = await memory.ImportTextAsync(@"문다솔

- 문다솔은 창의적이며 따뜻한 마음을 가진 젊은 디자이너입니다.
- 자연과 예술에 깊은 애정을 품고 있으며, 일상에서 새로운 영감을 끊임없이 찾아냅니다.
- 도전적인 프로젝트를 즐기며, 주변 사람들에게 긍정적인 에너지를 전파하는 인물입니다.");

            var memoryPlugin = kernel.ImportPluginFromObject(new MemoryPlugin(memory, waitForIngestionToComplete: true), "memory");

            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            };

            var question = "문다솔이 누구야?";
            var response = await kernel.InvokePromptAsync(question, new KernelArguments(settings));

            output.WriteLine(response.GetValue<string>());
        }


        [Fact]
        public async Task Tag()
        {
            var index = Guid.NewGuid().ToString().Replace("-", "");
            var memory = KernelMemoryGenerator.GetMemory();
            var docId = await memory.ImportDocumentAsync(new Document(index)
                .AddFile("file5-NASA-news.pdf")
                .AddTag("user", "Taylor")
                .AddTag("collection", "meetings")
                .AddTag("collection", "NASA")
                .AddTag("collection", "space")
                .AddTag("type", "news"), index: index);

            var question = "Any news from NASA about Orion?";

            var answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("user", "Blake"));
            output.WriteLine($"\nBlake Answer (none expected): {answer.Result}");


            answer = await memory.AskAsync(question, filter: MemoryFilters.ByTag("user", "Taylor"));
            output.WriteLine($"\nTaylor Answer: {answer.Result}\n  Sources:\n");
        }



        [Fact]
        public async Task Chunking_300()
        {
            var index = "chunk_300";

            var memory = KernelMemoryGenerator.BuilderForChunk()
                .WithCustomTextPartitioningOptions(
                new TextPartitioningOptions
                {
                    MaxTokensPerParagraph = 300,
                    OverlappingTokens = 50
                }).Build<MemoryServerless>();

            var docId = await memory.ImportDocumentAsync("file4-KM-Readme.pdf", documentId: index, index: index);
            var question = "Kernel Memory가 뭐야?";

            await Answer(question, memory, index);
        }

        [Fact]
        public async Task Chunking_100()
        {
            var index = "chunk_100";

            var memory = KernelMemoryGenerator.BuilderForChunk()
                .WithCustomTextPartitioningOptions(
                new TextPartitioningOptions
                {
                    MaxTokensPerParagraph = 100,
                    OverlappingTokens = 20
                }).Build<MemoryServerless>();

            var docId = await memory.ImportDocumentAsync("file4-KM-Readme.pdf", documentId: index, index: index);
            var question = "Kernel Memory가 뭐야?";

            await Answer(question, memory, index);
        }



        [Fact]
        public async Task Download()
        {
            var index = Guid.NewGuid().ToString().Replace("-", "");
            var memory = KernelMemoryGenerator.GetMemory();

            var fileName = "file4-KM-Readme.pdf";
            var docId = await memory.ImportDocumentAsync(fileName, documentId: index, index: index);

            StreamableFileContent result = await memory.ExportFileAsync(documentId: docId, fileName: fileName, index: index);
            var stream = new MemoryStream();
            await (await result.GetStreamAsync()).CopyToAsync(stream);
            var bytes = stream.ToArray();

            output.WriteLine();
            output.WriteLine("원래 File name : " + fileName);
            output.WriteLine("원래 File size : " + new FileInfo(fileName).Length);
            output.WriteLine("원래 Bytes count: " + (await File.ReadAllBytesAsync(fileName)).Length);
            output.WriteLine();
            output.WriteLine("다운된 File name : " + result.FileName);
            output.WriteLine("다운된 File type : " + result.FileType);
            output.WriteLine("다운된 File size : " + result.FileSize);
            output.WriteLine("다운된 Bytes count: " + bytes.Length);
        }







        private async Task Answer(string question, IKernelMemory memory, string index)
        {
            output.WriteLine($"Question: {question}");

            output.WriteLine();
            output.WriteLine("Answer: ");

            var answer = await memory.AskAsync(question, index: index);
            output.WriteLine(answer.Result);

            output.WriteLine();
            output.WriteLine("참고: ");
            output.WriteLine(JsonSerializer.Serialize(answer.RelevantSources, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
        }
    }

    public class KernelMemoryGenerator
    {
        public static MemoryServerless GetMemory()
        {
            var memoryBuilder = new KernelMemoryBuilder()
            .WithAzureOpenAITextEmbeddingGeneration(new AzureOpenAIConfig
            {
                APIKey = Configuration.AzureOpenAIApiKey,
                Endpoint = Configuration.AzureOpenAIEndpoint,
                Deployment = Configuration.TextEmbeddingDeploymentName,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey,
            })
            .WithAzureOpenAITextGeneration(new AzureOpenAIConfig
            {
                APIKey = Configuration.AzureOpenAIApiKey,
                Endpoint = Configuration.AzureOpenAIEndpoint,
                Deployment = Configuration.TextGenerationDeploymentName,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey
            })
            .WithSimpleVectorDb()
            .WithSimpleFileStorage();

            var memory = memoryBuilder.Build<MemoryServerless>();
            return memory;
        }
        
        public static IKernelMemoryBuilder BuilderForChunk()
        {
            var memoryBuilder = new KernelMemoryBuilder()
            .WithAzureOpenAITextEmbeddingGeneration(new AzureOpenAIConfig
            {
                APIKey = Configuration.AzureOpenAIApiKey,
                Endpoint = Configuration.AzureOpenAIEndpoint,
                Deployment = Configuration.TextEmbeddingDeploymentName,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey,
            })
            .WithAzureOpenAITextGeneration(new AzureOpenAIConfig
            {
                APIKey = Configuration.AzureOpenAIApiKey,
                Endpoint = Configuration.AzureOpenAIEndpoint,
                Deployment = Configuration.TextGenerationDeploymentName,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey
            })
            .WithSimpleVectorDb(new SimpleVectorDbConfig { StorageType = FileSystemTypes.Disk })
            .WithSimpleFileStorage(new SimpleFileStorageConfig
            {
                Directory = "tmp-memory-files",
                StorageType = FileSystemTypes.Disk
            });

            return memoryBuilder;
        }
    }
}