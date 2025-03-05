using GuidedProject.ApiApp.Models;
using GuidedProject.ApiApp.Services;

using Microsoft.AspNetCore.Mvc;

namespace GuidedProject.ApiApp.Endpoints;

public static class ChatCompletionEndpoint
{
    public static IEndpointRouteBuilder MapChatCompletionEndpoint(this IEndpointRouteBuilder routeBuilder)
    {
        var api = routeBuilder.MapGroup("api/chat");

        api.MapPost("complete", PostChatCompletionAsync)
           .Accepts<PromptRequest>(contentType: "application/json")
           .Produces<IEnumerable<PromptResponse>>(statusCode: StatusCodes.Status200OK, contentType: "application/json")
           .WithTags("chat")
           .WithName("ChatCompletion")
           .WithOpenApi();

        return routeBuilder;
    }

    public static async IAsyncEnumerable<PromptResponse> PostChatCompletionAsync([FromBody] PromptRequest req, IKernelService service)
    {
        await Task.Delay(1000);

        var result = service.CompleteChatStreamingAsync(req.Prompt);

        await foreach (var text in result)
        {
            yield return new PromptResponse(text);
        }
    }
}
