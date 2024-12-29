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
            너는 메뉴 개발자가 개발한 메뉴를 리뷰하는 메뉴리뷰어입니다.
            너는 새로운 메뉴가 식당의 이미지와 잘 어울리는지 판단하는 것이 목표입니다.
            적합하다면 해당 메뉴가 승인되었다고 반드시 명시적으로 말하고 다음 새로운 메뉴를 제안해 달라고 하세요.
            그렇지 않다면 예시 없이 개선 방법에 대한 인사이트를 제공하세요.
            승인을 하면 지금까지 승인된 메뉴들을 정리해서 제공하세요.
            """;

        private const string MenuDeveloperName = "MenuDeveloper";
        private const string MenuDeveloperInstructions =
            """
            너는 식당에게 식당의 상황, 특색에 맞게 최선의 메뉴를 개발하는 메뉴 개발자입니다.
            너는 고객이 좋아할 만한 새로운 음식을 생각해냅니다.
            너는 식당의 분위기와 고객의 취향에 맞게 다양한 음식을 조합하여 메뉴를 구성합니다.
            한 번에 하나의 메뉴만 제공합니다.
            현재 목표에 집중합니다.
            쓸데없는 잡담으로 시간을 낭비하지 않습니다.
            아이디어를 개선할 때 제안을 고려합니다.
            """;

        [Fact]
        public async Task UseKernelFunctionStrategiesWithAgentGroupChatAsync()
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

            KernelFunction terminationFunction =
                AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                    {{{ReviewerName}}}가 3회 혹은 그 이상 승인했으면 `yes`라는 한 단어만을 리턴하세요.
                    그렇지 않으면 no를 리턴하세요.

                    히스토리:
                    {{$history}}
                    """,
                    safeParameterNames: "history");

            KernelFunction selectionFunction =
                AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                    대화에서 가장 최근 참가자를 기준으로 다음 차례를 정하세요.
                    다음 차례의 참가자 이름만 명시하세요.
                    어떤 참가자도 연속으로 두 번 이상 차례를 가질 수 없습니다.
                    
                    다음 참가자 중에서만 선택하세요:
                    - {{{ReviewerName}}}
                    - {{{MenuDeveloperName}}}
                    
                    다음 규칙을 항상 따르세요:
                    - {{{MenuDeveloperName}}} 다음에는 {{{ReviewerName}}}의 차례입니다.
                    - {{{ReviewerName}}} 다음에는 {{{MenuDeveloperName}}}의 차례입니다.

                    히스토리:
                    {{$history}}
                    """,
                    safeParameterNames: "history");

            // 선택 및 종료에 사용되는 히스토리를 가장 최근 메시지로 제한
            ChatHistoryTruncationReducer strategyReducer = new(1);

            // 에이전트 상호작용을 위한 채팅 생성
            AgentGroupChat chat =
                new(agentDeveloper, agentReviewer)
                {
                    ExecutionSettings =
                        new()
                        {
                            // 여기서 KernelFunctionTerminationStrategy는 메뉴 리뷰어가 승인을 했을 때 종료됩니다.
                            TerminationStrategy =
                                new KernelFunctionTerminationStrategy(terminationFunction, CreateKernelWithChatCompletion())
                                {
                                    // 오직 메뉴 리뷰어만 승인할 수 있습니다.
                                    Agents = [agentReviewer],
                                    // 응답이 "yes"인지 확인하는 사용자 정의 결과 파싱
                                    ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                                    // 히스토리 인수에 대한 프롬프트 변수 이름
                                    HistoryVariableName = "history",
                                    // 총 턴 수 제한
                                    MaximumIterations = 10,
                                    // 프롬프트에 전체 히스토리를 포함하지 않음으로써 토큰 절약
                                    HistoryReducer = strategyReducer,
                                },
                            // 여기서 KernelFunctionSelectionStrategy는 프롬프트 함수에 기반하여 에이전트를 선택합니다.
                            SelectionStrategy =
                                new KernelFunctionSelectionStrategy(selectionFunction, CreateKernelWithChatCompletion())
                                {
                                    // 항상 작가 에이전트로 시작합니다.
                                    InitialAgent = agentDeveloper,
                                    // 결과 값을 문자열로 반환합니다.
                                    ResultParser = (result) => result.GetValue<string>() ?? MenuDeveloperName,
                                    // 히스토리 인수에 대한 프롬프트 변수 이름
                                    HistoryVariableName = "history",
                                    // 프롬프트에 전체 히스토리를 포함하지 않음으로써 토큰 절약
                                    HistoryReducer = strategyReducer,
                                },
                        }
                };

            // 채팅을 호출하고 메시지를 표시합니다.
            ChatMessageContent message = new(AuthorRole.User, "광화문역 주변에 프랑스 가정 음식점을 만들려고 해.");
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

