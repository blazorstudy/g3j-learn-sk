using System.ComponentModel;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenAI.Chat;

using Xunit.Abstractions;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace MultiAgents
{
    public class Demo2_02(ITestOutputHelper output) : BaseAgentsTest(output)
    {
        private const string HostName = "Host";
        private const string HostInstructions = "메뉴에 대한 질문에 답변하세요.";

        [Fact]
        public async Task UseChatCompletionWithPluginAgentAsync()
        {
            // 에이전트 정의
            ChatCompletionAgent agent =
                new()
                {
                    Instructions = HostInstructions,
                    Name = HostName,
                    Kernel = this.CreateKernelWithChatCompletion(),
                    Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
                };

            // 플러그인 초기화 및 에이전트의 커널에 추가 (직접 커널 사용과 동일).
            KernelPlugin plugin = KernelPluginFactory.CreateFromType<MenuPlugin>();
            agent.Kernel.Plugins.Add(plugin);

            /// 에이전트 상호작용을 캡처하기 위한 채팅 기록 생성.
            ChatHistory chat = [];

            // 사용자 입력에 응답하고 적절한 경우 함수를 호출.
            await InvokeAgentAsync("안녕하세요");
            await InvokeAgentAsync("오늘의 스페셜 수프는 무엇인가요?");
            await InvokeAgentAsync("오늘의 스페셜 음료는 무엇인가요?");
            await InvokeAgentAsync("감사합니다");

            // 에이전트를 호출하고 대화 메시지를 표시.
            async Task InvokeAgentAsync(string input)
            {
                ChatMessageContent message = new(AuthorRole.User, input);
                chat.Add(message);
                this.WriteAgentChatMessage(message);

                await foreach (ChatMessageContent response in agent.InvokeAsync(chat))
                {
                    chat.Add(response);

                    this.WriteAgentChatMessage(response);
                }
            }
        }

        private sealed class MenuPlugin
        {
            [KernelFunction, Description("메뉴의 특별 항목 목록을 제공합니다.")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:적절한 경우 속성을 사용하세요", Justification = "너무 스마트함")]
            public string GetSpecials() =>
                """
                스페셜 수프: 클램 차우더
                스페셜 샐러드: 콥 샐러드
                스페셜 음료: 차이 티
                """;

            [KernelFunction, Description("요청된 메뉴 항목의 가격을 제공합니다.")]
            public string GetItemPrice(
                [Description("메뉴 항목의 이름.")]
                string menuItem) =>
                "10,000원";
        }
    }
}