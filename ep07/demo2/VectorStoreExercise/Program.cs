using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System.Net;
using VectorStoreExercise;

var vectorStore = new VectorStoreService();
await vectorStore.Load();
var searchPlugin = vectorStore.TextSearch.CreateWithGetTextSearchResults("SearchPlugin");

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion("gpt-4o", Credential.EndPoint, Credential.ApiKey);

var kernel = builder.Build();

kernel.Plugins.Add(searchPlugin);
var chatService = kernel.Services.GetService<IChatCompletionService>();
var chatHistory = new ChatHistory();

while (true)
{
    Console.Write("User : ");
    var input = Console.ReadLine();
    Console.WriteLine();

    await Input1(input);
}

async Task Input1(string query)
{
    chatHistory.AddUserMessage(query);

    var searchResults = await vectorStore.Search(query);
    var searchEmbeddingResult = string.Empty;

    foreach (var searchResult in searchResults)
    {
        searchEmbeddingResult += searchResult.Record.EmbeddingData;
        searchEmbeddingResult += $"Link : {searchResult.Record.Link}";
        searchEmbeddingResult += Environment.NewLine;
        searchEmbeddingResult += Environment.NewLine;
    }

    string promptTemplate = $"""
            {searchEmbeddingResult}  

            {query}

            응답에서 참조된 정보와 관련된 인용을 포함하세요.

            """;

    KernelArguments arguments = new() { { "query", query } };
    HandlebarsPromptTemplateFactory promptTemplateFactory = new();

    var result = kernel.InvokePromptStreamingAsync(
            promptTemplate,
            arguments,
            templateFormat: HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
            promptTemplateFactory: promptTemplateFactory);

    Console.Write("Assistant : ");
    var assistantMsg = string.Empty;
    await foreach (var text in result)
    {
        await Task.Delay(20);
        assistantMsg += text;
        Console.Write(text);
    }

    Console.WriteLine();
    Console.WriteLine();
}

async Task Input(string query)
{
    chatHistory.AddUserMessage(query);

    string promptTemplate = """
            {{#with (SearchPlugin-GetTextSearchResults query)}}  
              {{#each this}}  
                Name: {{Name}}
                Value: {{Value}}
                Link: {{Link}}
                -----------------
              {{/each}}  
            {{/with}}  

            {{query}}

            응답에서 참조된 정보와 관련된 인용을 포함하세요.

            """;
    KernelArguments arguments = new() { { "query", query } };
    HandlebarsPromptTemplateFactory promptTemplateFactory = new();

    var result = kernel.InvokePromptStreamingAsync(
            promptTemplate,
            arguments,
            templateFormat: HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
            promptTemplateFactory: promptTemplateFactory);

    Console.Write("Assistant : ");
    var assistantMsg = string.Empty;
    await foreach (var text in result)
    {
        await Task.Delay(20);
        assistantMsg += text;
        Console.Write(text);
    }

    Console.WriteLine();
    Console.WriteLine();
}
