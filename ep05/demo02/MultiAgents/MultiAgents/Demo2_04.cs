using System.ComponentModel;

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
    public class Demo2_04(ITestOutputHelper output) : BaseAgentsTest(output)
    {
        private const int ScoreCompletionThreshold = 70;

        private const string TutorName = "Tutor";
        private const string TutorInstructions =
            """
        Think step-by-step and rate the user input on creativity and expressiveness from 1-100.

        Respond in JSON format with the following JSON schema:

        {
            "score": "integer (1-100)",
            "notes": "the reason for your score"
        }
        """;

        [Fact]
        public async Task UseKernelFunctionStrategiesWithJsonResultAsync()
        {
            // ������Ʈ�� �����մϴ�.
            ChatCompletionAgent agent =
                new()
                {
                    Instructions = TutorInstructions,
                    Name = TutorName,
                    Kernel = this.CreateKernelWithChatCompletion(),
                };

            // ������Ʈ ��ȣ�ۿ��� ���� ä���� �����մϴ�.
            AgentGroupChat chat =
                new()
                {
                    ExecutionSettings =
                        new()
                        {
                            // ���⼭ TerminationStrategy ���� Ŭ������ ���信 ������ 70 �̻��� ��� ����˴ϴ�.
                            TerminationStrategy = new ThresholdTerminationStrategy()
                        }
                };

            // ����� �Է¿� �����մϴ�.
            await InvokeAgentAsync("�ٶ��� �ִϴ�.");
            await InvokeAgentAsync("�ٶ��� ���� ���̷� �Ҿ�ɴϴ�.");
            await InvokeAgentAsync("�ٶ��� ���� ���̷� �Ҿ�� �ٻ�͵��� ���� �ߵ� ��鸳�ϴ�.");
            

            // ������Ʈ�� ȣ���ϰ� ��ȭ �޽����� ǥ��
            async Task InvokeAgentAsync(string input)
            {
                ChatMessageContent message = new(AuthorRole.User, input);
                chat.AddChatMessage(message);
                this.WriteAgentChatMessage(message);

                await foreach (ChatMessageContent response in chat.InvokeAsync(agent))
                {
                    this.WriteAgentChatMessage(response); 
                }

                Console.WriteLine($"[IS COMPLETED: {chat.IsComplete}]");
            }
        }

        private record struct WritingScore(int score, string notes);

        private sealed class ThresholdTerminationStrategy : TerminationStrategy
        {
            protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            {
                string lastMessageContent = history[history.Count - 1].Content ?? string.Empty;

                WritingScore? result = JsonResultTranslator.Translate<WritingScore>(lastMessageContent);

                return Task.FromResult((result?.score ?? 0) >= ScoreCompletionThreshold);
            }
        }
    }
}

