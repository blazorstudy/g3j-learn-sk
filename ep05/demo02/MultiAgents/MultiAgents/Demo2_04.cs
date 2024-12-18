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
            // 에이전트를 정의합니다.
            ChatCompletionAgent agent =
                new()
                {
                    Instructions = TutorInstructions,
                    Name = TutorName,
                    Kernel = this.CreateKernelWithChatCompletion(),
                };

            // 에이전트 상호작용을 위한 채팅을 생성합니다.
            AgentGroupChat chat =
                new()
                {
                    ExecutionSettings =
                        new()
                        {
                            // 여기서 TerminationStrategy 하위 클래스는 응답에 점수가 70 이상인 경우 종료됩니다.
                            TerminationStrategy = new ThresholdTerminationStrategy()
                        }
                };

            // 사용자 입력에 응답합니다.
            await InvokeAgentAsync("바람이 붑니다.");
            await InvokeAgentAsync("바람이 나무 사이로 불어옵니다.");
            await InvokeAgentAsync("바람이 나무 사이로 불어와 잎사귀들이 춤을 추듯 흔들립니다.");
            

            // 에이전트를 호출하고 대화 메시지를 표시
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

