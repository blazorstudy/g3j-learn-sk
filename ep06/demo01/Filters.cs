using Microsoft.SemanticKernel;

namespace demo01;

public class PromptFilter1 : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} PromptFilter1");
        
        await next(context);

        context.RenderedPrompt = context.RenderedPrompt + "\n 한 문장으로 표현해줘.";
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} PromptFilter1 ${context.RenderedPrompt}\n");
    }
}

public class PromptFilter2 : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} PromptFilter2");

        await next(context);

        context.RenderedPrompt = context.RenderedPrompt + "\n 한글로 표현해줘.";
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} PromptFilter2 ${context.RenderedPrompt}\n");
    } 
}

public class FunctionFilter1 : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} FunctionFilter1");
        
        await next(context);

        //Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} FunctionFilter1에서 답변을 변경합니다.");
        //context.Result = new FunctionResult(context.Function, value: "G3J를 보는 것이에요!!!");
    }
}

public class FunctionFilter2 : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} FunctionFilter2");

        await next(context);

        if (context.IsStreaming)
        {
            Console.WriteLine($"DateTime.Now.ToString(\"yyyy-MM-dd HH:mm:ss.fff\")}} streaming");
        }
        else
        {
            Console.WriteLine($"DateTime.Now.ToString(\"yyyy-MM-dd HH:mm:ss.fff\")}} non-streaming");
        }
        Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} FunctionFilter2 {context.Result}");
    }
}
