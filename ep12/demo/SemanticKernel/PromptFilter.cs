using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;

namespace demo.SemanticKernel;

public class PromptFilter : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        await next(context);

        context.RenderedPrompt = context.RenderedPrompt + $"\n 오늘은 {DateTime.Today}이야.";
    }
}
