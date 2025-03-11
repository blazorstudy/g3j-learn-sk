using System.Reflection;

using GuidedProject.ApiApp.Plugins.RestaurantAgent;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace GuidedProject.ApiApp.Services;

public interface IKernelService
{
    IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt);
    IAsyncEnumerable<string> CompleteChatStreamingAsync(IEnumerable<ChatMessageContent> messages);
}

public class KernelService(Kernel kernel, IConfiguration config) : IKernelService
{
    public async IAsyncEnumerable<string> CompleteChatStreamingAsync(string prompt)
    {
        var settings = new PromptExecutionSettings { ServiceId = config["SemanticKernel:ServiceId"] };
        var arguments = new KernelArguments(settings);

        var result = kernel.InvokePromptStreamingAsync(prompt, arguments).ConfigureAwait(false);

        await foreach (var text in result)
        {
            yield return text.ToString();
        }
    }

    public async IAsyncEnumerable<string> CompleteChatStreamingAsync(IEnumerable<ChatMessageContent> messages)
    {
        await foreach (var text in this.InvokeChatMessageContentsAsync(messages))
        // await foreach (var text in this.InvokeStoryTellerAgentAsync(messages))
        // await foreach (var text in this.InvokeRestaurantServerAgentAsync(messages))
        // await foreach (var text in this.InvokeAgentCollaborationsAsync(messages))
        {
            yield return text;
        }
    }

    private async IAsyncEnumerable<string> InvokeChatMessageContentsAsync(IEnumerable<ChatMessageContent> messages)
    {
        var history = new ChatHistory();
        history.AddRange(messages);

        var service = kernel.GetRequiredService<IChatCompletionService>(config["SemanticKernel:ServiceId"]!);

        var result = service.GetStreamingChatMessageContentsAsync(chatHistory: history, kernel: kernel);
        await foreach (var text in result)
        {
            yield return text.ToString();
        }
    }

    private async IAsyncEnumerable<string> InvokeStoryTellerAgentAsync(IEnumerable<ChatMessageContent> messages)
    {
        var definition = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Plugins", "StoryTellerAgent", "StoryTeller.yaml"));
        var template = KernelFunctionYaml.ToPromptTemplateConfig(definition);
        var agent = new ChatCompletionAgent(template, new KernelPromptTemplateFactory())
                    {
                        Kernel = kernel
                    };
        var settings = new PromptExecutionSettings()
        {
            ServiceId = config["SemanticKernel:ServiceId"]
        };
        var arguments = new KernelArguments(settings)
        {
            { "topic", messages.Last().Content },
            { "length", 3 }
        };
        var history = new ChatHistory();
        history.AddRange(messages);

        var result = agent.InvokeStreamingAsync(history, arguments);
        await foreach (var text in result)
        {
            yield return text.ToString();
        }
    }

    private async IAsyncEnumerable<string> InvokeRestaurantServerAgentAsync(IEnumerable<ChatMessageContent> messages)
    {
        var plugin = kernel.Plugins.SingleOrDefault(p => p.Name == nameof(MenuPlugin));
        if (plugin == null)
        {
            kernel.Plugins.AddFromType<MenuPlugin>();
        }

        var settings = new PromptExecutionSettings()
        {
            ServiceId = config["SemanticKernel:ServiceId"],
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var agent = new ChatCompletionAgent()
        {
            Name = "Host",
            Instructions = "Answer questions about the menu.",
            Kernel = kernel,
            Arguments = new KernelArguments(settings),
        };
        var history = new ChatHistory();
        history.AddRange(messages);

        var result = agent.InvokeStreamingAsync(history);
        await foreach (var text in result)
        {
            yield return text.ToString();
        }
    }

    private async IAsyncEnumerable<string> InvokeAgentCollaborationsAsync(IEnumerable<ChatMessageContent> messages)
    {
        var reviewerName = "ProjectManager";
        var reviewerInstructions =
            """
            You are a project manager who has opinions about copywriting born of a love for David Ogilvy.
            The goal is to determine if the given copy is acceptable to print.
            If so, state that it is approved.
            If not, provide insight on how to refine suggested copy without examples.
            """;

        var copywriterName = "Copywriter";
        var copywriterInstructions =
            """
            You are a copywriter with ten years of experience and are known for brevity and a dry humor.
            The goal is to refine and decide on the single best copy as an expert in the field.
            Only provide a single proposal per response.
            Never delimit the response with quotation marks.
            You're laser focused on the goal at hand.
            Don't waste time with chit chat.
            Consider suggestions when refining an idea.
            """;

        var agentReviewer = new ChatCompletionAgent()
        {
            Name = reviewerName,
            Instructions = reviewerInstructions,
            Kernel = kernel
        };

        var agentWriter = new ChatCompletionAgent()
        {
            Name = copywriterName,
            Instructions = copywriterInstructions,
            Kernel = kernel
        };

        var terminationFunction =
            AgentGroupChat.CreatePromptFunctionForStrategy(
                """
                Determine if the copy has been approved. If so, respond with a single word: yes

                History:
                {{$history}}
                """,
                safeParameterNames: "history");

        var selectionFunction =
            AgentGroupChat.CreatePromptFunctionForStrategy(
                $$$"""
                Determine which participant takes the next turn in a conversation based on the the most recent participant.
                State only the name of the participant to take the next turn.
                No participant should take more than one turn in a row.
                
                Choose only from these participants:
                - {{{reviewerName}}}
                - {{{copywriterName}}}
                
                Always follow these rules when selecting the next participant:
                - After {{{copywriterName}}}, it is {{{reviewerName}}}'s turn.
                - After {{{reviewerName}}}, it is {{{copywriterName}}}'s turn.

                History:
                {{$history}}
                """,
                safeParameterNames: "history");

        // Limit history used for selection and termination to the most recent message.
        var strategyReducer = new ChatHistoryTruncationReducer(1);

        var chat = new AgentGroupChat(agentWriter, agentReviewer)
        {
            ExecutionSettings = new AgentGroupChatSettings()
            {
                // Here a KernelFunctionSelectionStrategy selects agents based on a prompt function.
                SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, kernel)
                {
                    // Always start with the writer agent.
                    InitialAgent = agentWriter,
                    // Returns the entire result value as a string.
                    ResultParser = (result) => result.GetValue<string>() ?? copywriterName,
                    // The prompt variable name for the history argument.
                    HistoryVariableName = "history",
                    // Save tokens by not including the entire history in the prompt
                    HistoryReducer = strategyReducer,
                    // Only include the agent names and not the message content
                    EvaluateNameOnly = true,
                },
                // Here KernelFunctionTerminationStrategy will terminate when the art-director has given their approval.
                TerminationStrategy = new KernelFunctionTerminationStrategy(terminationFunction, kernel)
                {
                    // Only the art-director may approve.
                    Agents = [ agentReviewer ],
                    // Customer result parser to determine if the response is "yes"
                    ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.InvariantCultureIgnoreCase) ?? false,
                    // The prompt variable name for the history argument.
                    HistoryVariableName = "history",
                    // Limit total number of turns
                    MaximumIterations = 10,
                    // Save tokens by not including the entire history in the prompt
                    HistoryReducer = strategyReducer,
                    // Reset for restart
                    AutomaticReset = true,
                },
            }
        };

        var history = new ChatHistory();
        history.AddRange(messages);

        var agentName = default(string);
        var isAgentChanged = false;
        var response = chat.InvokeStreamingAsync();
        await foreach (var content in response)
        {
            if (content.AuthorName?.Equals(agentName, StringComparison.InvariantCultureIgnoreCase) == false)
            {
                isAgentChanged = true;
                agentName = content.AuthorName;
            }
            else
            {
                isAgentChanged = false;
            }

            yield return isAgentChanged ? $"\n\n{content.AuthorName!}\n------\n{content}" : content.ToString();
        }

    }
}
