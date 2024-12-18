using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

using OpenAI.Chat;

using Xunit.Abstractions;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace MultiAgents
{
    public class Demo2_01(ITestOutputHelper output) : BaseAgentsTest(output)
    {
        private const string ReviewerName = "MenuReviewer";
        private const string ReviewerInstructions =
            """
        �ʴ� �޴� �����ڰ� ������ �޴��� �����ϴ� �޴�������Դϴ�.
        �ʴ� ���ο� �޴��� �Ĵ��� �̹����� �� ��︮���� �Ǵ��ϴ� ���� ��ǥ�Դϴ�.
        �����ϴٸ� ���εǾ��ٰ� ��������� ���ϼ���.
        �׷��� �ʴٸ� ���� ���� ���� ����� ���� �λ���Ʈ�� �����ϼ���.
        """;

        private const string MenuDeveloperName = "MenuDeveloper";
        private const string MenuDeveloperInstructions =
            """
        �ʴ� �Ĵ翡�� �Ĵ��� ��Ȳ, Ư���� �°� �ּ��� �޴��� �����ϴ� �޴� �������Դϴ�.
        �ʴ� ���� ������ ���� ���ο� ������ �����س��ϴ�.
        �ʴ� �Ĵ��� ������� ���� ���⿡ �°� �پ��� ������ �����Ͽ� �޴��� �����մϴ�.
        �� ���� �ϳ��� ���ȸ� �����մϴ�.
        ���� ��ǥ�� �����մϴ�.
        �������� ������� �ð��� �������� �ʽ��ϴ�.
        ���̵� ������ �� ������ ����մϴ�.
        """;

        [Fact]
        public async Task UseAgentGroupChatWithTwoAgentsAsync()
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

            // ������Ʈ ��ȣ�ۿ��� ���� ä�� ����
            AgentGroupChat chat =
                new(agentDeveloper, agentReviewer)
                {
                    ExecutionSettings =
                        new()
                        {
                            // TerminatorStrategy�� ����Ͽ� ä�� ���� ������ �����մϴ�.
                            // ��ý���Ʈ�� �����̶�� �ܾ �����ϸ� ä���� ����˴ϴ�.
                            TerminationStrategy =
                                new ApprovalTerminationStrategy()
                                {
                                    // MenuReviewer�� �̸� ������ �� ����
                                    Agents = [agentReviewer],
                                    // �ִ� 10ȸ���� �ݺ�
                                    MaximumIterations = 10,
                                }
                        }
                };

            // ä���� �����ϰ� ����� ����մϴ�.
            ChatMessageContent input = new(AuthorRole.User, "��ȭ���� �ֺ��� ������ ���� �������� ������� ��.");
            chat.AddChatMessage(input);
            this.WriteAgentChatMessage(input);

            await foreach (ChatMessageContent response in chat.InvokeAsync())
            {
                this.WriteAgentChatMessage(response);
            }

            Console.WriteLine($"\n[IS COMPLETED: {chat.IsComplete}]");
        }

        private sealed class ApprovalTerminationStrategy : TerminationStrategy
        {
            // �޽����� "����"�̶�� �ܾ ���ԵǸ� AgentGroupChat�� �����մϴ�.
            protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
                => Task.FromResult((history[history.Count - 1].Content?.Contains("����", StringComparison.OrdinalIgnoreCase) == true 
                    || history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) == true));
        }
    }
}