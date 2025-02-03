#pragma warning disable SKEXP0001 // 형식은 평가 목적으로 제공되며, 이후 업데이트에서 변경되거나 제거될 수 있습니다. 계속하려면 이 진단을 표시하지 않습니다.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Apis.CustomSearchAPI.v1.Data;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace demo02;

public class PromptFilter : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        await next(context);
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} PromptFilter2 : {context.RenderedPrompt}\n");
    }
}

public class AutoFunctionFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        // Call the function first.
        await next(context);

        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} AutoFunctionFilter : {context.Function.Name}");

        if (context.Function.Name == "GetTextSearchResults")
        {
            var results = context.Result.GetValue<List<TextSearchResult>>();
            foreach (var result in results)
            {
                Console.WriteLine($"Name:  {result.Name}");
                Console.WriteLine($"Value: {result.Value}");
                Console.WriteLine($"Link:  {result.Link}");
                Console.WriteLine();
            }
        }
    }
}

public class FunctionFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        await next(context);

        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} FunctionFilter : {context.Function.Name}");

        if (context.Function.Name == "GetTextSearchResults")
        {
            Console.WriteLine(context.Result);

            var results = context.Result.GetValue<List<TextSearchResult>>();
            foreach (var result in results)
            {
                Console.WriteLine($"Name:  {result.Name}");
                Console.WriteLine($"Value: {result.Value}");
                Console.WriteLine($"Link:  {result.Link}");
                Console.WriteLine();
            }
        }
    }
}