using System.ComponentModel;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.History;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenAI.Chat;

using Xunit.Abstractions;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace MultiAgents
{
    public class Demo2_03(ITestOutputHelper output) : BaseAgentsTest(output)
    {
        private const string ReviewerName = "MenuReviewer";
        private const string ReviewerInstructions =
            """
            �ʴ� �޴� �����ڰ� ������ �޴��� �����ϴ� �޴�������Դϴ�.
            �ʴ� ���ο� �޴��� �Ĵ��� �̹����� �� ��︮���� �Ǵ��ϴ� ���� ��ǥ�Դϴ�.
            �����ϴٸ� �ش� �޴��� ���εǾ��ٰ� �ݵ�� ��������� ���ϰ� ���� ���ο� �޴��� ������ �޶�� �ϼ���.
            �׷��� �ʴٸ� ���� ���� ���� ����� ���� �λ���Ʈ�� �����ϼ���.
            ������ �ϸ� ���ݱ��� ���ε� �޴����� �����ؼ� �����ϼ���.
            """;

        private const string MenuDeveloperName = "MenuDeveloper";
        private const string MenuDeveloperInstructions =
            """
            �ʴ� �Ĵ翡�� �Ĵ��� ��Ȳ, Ư���� �°� �ּ��� �޴��� �����ϴ� �޴� �������Դϴ�.
            �ʴ� ���� ������ ���� ���ο� ������ �����س��ϴ�.
            �ʴ� �Ĵ��� ������� ���� ���⿡ �°� �پ��� ������ �����Ͽ� �޴��� �����մϴ�.
            �� ���� �ϳ��� �޴��� �����մϴ�.
            ���� ��ǥ�� �����մϴ�.
            �������� ������� �ð��� �������� �ʽ��ϴ�.
            ���̵� ������ �� ������ ����մϴ�.
            """;

        [Fact]
        public async Task UseKernelFunctionStrategiesWithAgentGroupChatAsync()
        {
            // ������Ʈ ����
            ChatCompletionAgent agentReviewer =
                new()
                {
                    Instructions = ReviewerInstructions,
                    Name = ReviewerName,
                    Kernel = this.CreateKernelWithChatCompletion(),
                };

            ChatCompletionAgent agentDeveloper =
                new()
                {
                    Instructions = MenuDeveloperInstructions,
                    Name = MenuDeveloperName,
                    Kernel = this.CreateKernelWithChatCompletion(),
                };

            KernelFunction terminationFunction =
                AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                    {{{ReviewerName}}}�� 3�� Ȥ�� �� �̻��� ���������� `yes`��� �� �ܾ�� �����ϼ���.
                    �׷��� ������ no�� �����ϼ���.

                    �����丮:
                    {{$history}}
                    """,
                    safeParameterNames: "history");

            KernelFunction selectionFunction =
                AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                    ��ȭ���� ���� �ֱ� �����ڸ� �������� ���� ���ʸ� ���ϼ���.
                    ���� ������ ������ �̸��� ����ϼ���.
                    � �����ڵ� �������� �� �� �̻� ���ʸ� ���� �� �����ϴ�.
                    
                    ���� ������ �߿����� �����ϼ���:
                    - {{{ReviewerName}}}
                    - {{{MenuDeveloperName}}}
                    
                    ���� ��Ģ�� �׻� ��������:
                    - {{{MenuDeveloperName}}} �������� {{{ReviewerName}}}�� �����Դϴ�.
                    - {{{ReviewerName}}} �������� {{{MenuDeveloperName}}}�� �����Դϴ�.

                    �����丮:
                    {{$history}}
                    """,
                    safeParameterNames: "history");

            // ���� �� ���ῡ ���Ǵ� �����丮�� ���� �ֱ� �޽����� ����
            ChatHistoryTruncationReducer strategyReducer = new(1);

            // ������Ʈ ��ȣ�ۿ��� ���� ä�� ����
            AgentGroupChat chat =
                new(agentDeveloper, agentReviewer)
                {
                    ExecutionSettings =
                        new()
                        {
                            // ���⼭ KernelFunctionTerminationStrategy�� �޴� ���� ������ ���� �� ����˴ϴ�.
                            TerminationStrategy =
                                new KernelFunctionTerminationStrategy(terminationFunction, CreateKernelWithChatCompletion())
                                {
                                    // ���� �޴� ���� ������ �� �ֽ��ϴ�.
                                    Agents = [agentReviewer],
                                    // ������ "yes"���� Ȯ���ϴ� ����� ���� ��� �Ľ�
                                    ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                                    // �����丮 �μ��� ���� ������Ʈ ���� �̸�
                                    HistoryVariableName = "history",
                                    // �� �� �� ����
                                    MaximumIterations = 10,
                                    // ������Ʈ�� ��ü �����丮�� �������� �������ν� ��ū ����
                                    HistoryReducer = strategyReducer,
                                },
                            // ���⼭ KernelFunctionSelectionStrategy�� ������Ʈ �Լ��� ����Ͽ� ������Ʈ�� �����մϴ�.
                            SelectionStrategy =
                                new KernelFunctionSelectionStrategy(selectionFunction, CreateKernelWithChatCompletion())
                                {
                                    // �׻� �۰� ������Ʈ�� �����մϴ�.
                                    InitialAgent = agentDeveloper,
                                    // ��� ���� ���ڿ��� ��ȯ�մϴ�.
                                    ResultParser = (result) => result.GetValue<string>() ?? MenuDeveloperName,
                                    // �����丮 �μ��� ���� ������Ʈ ���� �̸�
                                    HistoryVariableName = "history",
                                    // ������Ʈ�� ��ü �����丮�� �������� �������ν� ��ū ����
                                    HistoryReducer = strategyReducer,
                                },
                        }
                };

            // ä���� ȣ���ϰ� �޽����� ǥ���մϴ�.
            ChatMessageContent message = new(AuthorRole.User, "��ȭ���� �ֺ��� ������ ���� �������� ������� ��.");
            chat.AddChatMessage(message);
            this.WriteAgentChatMessage(message);

            await foreach (ChatMessageContent responese in chat.InvokeAsync())
            {
                this.WriteAgentChatMessage(responese);
            }

            Console.WriteLine($"\n[IS COMPLETED: {chat.IsComplete}]");
        }
    }
}

