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
        너는 메뉴 개발자가 개발한 메뉴를 리뷰하는 메뉴리뷰어입니다.
        너는 새로운 메뉴가 식당의 이미지와 잘 어울리는지 판단하는 것이 목표입니다.
        적합하다면 승인되었다고 명시적으로 말하세요.
        그렇지 않다면 예시 없이 개선 방법에 대한 인사이트를 제공하세요.
        """;

        private const string MenuDeveloperName = "MenuDeveloper";
        private const string MenuDeveloperInstructions =
            """
        너는 식당에게 식당의 상황, 특색에 맞게 최선의 메뉴를 개발하는 메뉴 개발자입니다.
        너는 고객이 좋아할 만한 새로운 음식을 생각해냅니다.
        너는 식당의 분위기와 고객의 취향에 맞게 다양한 음식을 조합하여 메뉴를 구성합니다.
        한 번에 하나의 제안만 제공합니다.
        현재 목표에 집중합니다.
        쓸데없는 잡담으로 시간을 낭비하지 않습니다.
        아이디어를 개선할 때 제안을 고려합니다.
        """;

        [Fact]
        public async Task UseAgentGroupChatWithTwoAgentsAsync()
        {
            // 에이전트 정의
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

            // 에이전트 상호작용을 위한 채팅 생성
            AgentGroupChat chat =
                new(agentDeveloper, agentReviewer)
                {
                    ExecutionSettings =
                        new()
                        {
                            // TerminatorStrategy를 사용하여 채팅 종료 조건을 설정합니다.
                            // 어시스턴트가 승인이라는 단어를 응답하면 채팅이 종료됩니다.
                            TerminationStrategy =
                                new ApprovalTerminationStrategy()
                                {
                                    // MenuReviewer만 이를 승인할 수 있음
                                    Agents = [agentReviewer],
                                    // 최대 10회까지 반복
                                    MaximumIterations = 10,
                                }
                        }
                };

            // 채팅을 시작하고 결과를 출력합니다.
            ChatMessageContent input = new(AuthorRole.User, "광화문역 주변에 프랑스 가정 음식점을 만들려고 해.");
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
            // 메시지에 "승인"이라는 단어가 포함되면 AgentGroupChat을 종료합니다.
            protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
                => Task.FromResult((history[history.Count - 1].Content?.Contains("승인", StringComparison.OrdinalIgnoreCase) == true 
                    || history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) == true));
        }
    }
}