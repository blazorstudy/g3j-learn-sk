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
        private const string HostInstructions = "�޴��� ���� ������ �亯�ϼ���.";

        [Fact]
        public async Task UseChatCompletionWithPluginAgentAsync()
        {
            // ������Ʈ ����
            ChatCompletionAgent agent =
                new()
                {
                    Instructions = HostInstructions,
                    Name = HostName,
                    Kernel = this.CreateKernelWithChatCompletion(),
                    Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
                };

            // �÷����� �ʱ�ȭ �� ������Ʈ�� Ŀ�ο� �߰� (���� Ŀ�� ���� ����).
            KernelPlugin plugin = KernelPluginFactory.CreateFromType<MenuPlugin>();
            agent.Kernel.Plugins.Add(plugin);

            /// ������Ʈ ��ȣ�ۿ��� ĸó�ϱ� ���� ä�� ��� ����.
            ChatHistory chat = [];

            // ����� �Է¿� �����ϰ� ������ ��� �Լ��� ȣ��.
            await InvokeAgentAsync("�ȳ��ϼ���");
            await InvokeAgentAsync("������ ����� ������ �����ΰ���?");
            await InvokeAgentAsync("������ ����� ����� �����ΰ���?");
            await InvokeAgentAsync("�����մϴ�");

            // ������Ʈ�� ȣ���ϰ� ��ȭ �޽����� ǥ��.
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
            [KernelFunction, Description("�޴��� Ư�� �׸� ����� �����մϴ�.")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:������ ��� �Ӽ��� ����ϼ���", Justification = "�ʹ� ����Ʈ��")]
            public string GetSpecials() =>
                """
                ����� ����: Ŭ�� �����
                ����� ������: �� ������
                ����� ����: ���� Ƽ
                """;

            [KernelFunction, Description("��û�� �޴� �׸��� ������ �����մϴ�.")]
            public string GetItemPrice(
                [Description("�޴� �׸��� �̸�.")]
                string menuItem) =>
                "10,000��";
        }
    }
}