#pragma warning disable SKEXP0001, SKEXP0050

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.SemanticKernel.Plugins.Web.Google;

namespace demo02;

public class TextSearch
{
    /*
     * Google Web Search와 연결하여 검색 결과를 반환합니다.
     *  - 아직 Semantic Kernel에서 호출하지 않았습니다.
     *  - 단순히 textsearch를 Google로 할 수 있다. 여기까지입니다.
     */
    public async Task demo01(string engineId, string apiKey, string query )
    {
        var textSearch = new GoogleTextSearch(
                searchEngineId: engineId,
                apiKey: apiKey);

        // Search and return results
        KernelSearchResults<string> stringResults = await textSearch.SearchAsync(query, new() { Top = 4 });
        Console.WriteLine("\n--- String Results ---\n");
        await foreach (string result in stringResults.Results)
        {
            Console.WriteLine(result);
            Console.WriteLine();
        }

        KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(query, new() { Top = 4 });
        Console.WriteLine("\n--- Text Search Results ---\n");
        await foreach (TextSearchResult result in textResults.Results)
        {
            Console.WriteLine($"Name:  {result.Name}");
            Console.WriteLine($"Value: {result.Value}");
            Console.WriteLine($"Link:  {result.Link}");
            Console.WriteLine();
        }

        KernelSearchResults<object> fullResults = await textSearch.GetSearchResultsAsync(query, new() { Top = 4 });
        Console.WriteLine("\n--- Google Web Page Results ---\n");
        await foreach (Google.Apis.CustomSearchAPI.v1.Data.Result result in fullResults.Results)
        {
            Console.WriteLine($"Title:       {result.Title}");
            Console.WriteLine($"Snippet:     {result.Snippet}");
            Console.WriteLine($"Link:        {result.Link}");
            Console.WriteLine($"DisplayLink: {result.DisplayLink}");
            Console.WriteLine($"Kind:        {result.Kind}");
            Console.WriteLine();
        }
    }

    /*
     * Google Web Search와 연결하여 검색 결과를 반환합니다.
     *  - Semantic Kernel에서 자동으로 Google Web Search를 호출합니다.
     *  - Web Search 결과를 가지고 LLM에 Invoke Promt하는 것을 볼 수 있습니다.
     */
    public async Task demo02(string deployName, string endPoint, string key, string model, string engineId, string apiKey, string query)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(deployName, endPoint, key, model);
        var kernel = builder.Build();
        kernel.PromptRenderFilters.Add(new PromptFilter());
        kernel.FunctionInvocationFilters.Add(new FunctionFilter());
        //kernel.AutoFunctionInvocationFilters.Add(new AutoFunctionFilter());       // AutoFunctionFilter로 처리해도 됩니다.

        var textSearch = new GoogleTextSearch(
                                searchEngineId: engineId,
                                apiKey: apiKey);

        var plugin = textSearch.CreateWithGetTextSearchResults("GooglePlugin");
        kernel.Plugins.Add(plugin);

        OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
        KernelArguments arguments = new(settings);
        var response = await kernel.InvokePromptAsync(query, arguments);

        Console.WriteLine("\n응답:");
        Console.WriteLine(response);
        Console.WriteLine("\n");
    }
}
