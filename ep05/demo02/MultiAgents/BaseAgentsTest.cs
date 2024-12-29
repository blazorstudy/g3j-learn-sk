using System.ClientModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

using OpenAI.Assistants;

using OpenAI.Chat;

using OpenAI.Files;

using Xunit.Abstractions;

using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace MultiAgents
{
    public abstract class BaseAgentsTest : TextWriter
    {
        protected ITestOutputHelper Output { get; }

        protected ILoggerFactory LoggerFactory { get; }

        protected BaseAgentsTest(ITestOutputHelper output)
        {
            this.Output = output;
            //this.LoggerFactory = new XunitLogger(output);

            System.Console.SetOut(this);
        }
        /// <summary>
        /// Metadata key to indicate the assistant as created for a sample.
        /// </summary>
        protected const string AssistantSampleMetadataKey = "sksample";

        /// <summary>
        /// Metadata to indicate the assistant as created for a sample.
        /// </summary>
        /// <remarks>
        /// While the samples do attempt delete the assistants it creates, it is possible
        /// that some assistants may remain.  This metadata can be used to identify and sample
        /// agents for clean-up.
        /// </remarks>
        protected static readonly ReadOnlyDictionary<string, string> AssistantSampleMetadata =
            new(new Dictionary<string, string>
            {
            { AssistantSampleMetadataKey, bool.TrueString }
            });

        /// <summary>
        /// Common method to write formatted agent chat content to the console.
        /// </summary>
        protected void WriteAgentChatMessage(ChatMessageContent message)
        {
            // Include ChatMessageContent.AuthorName in output, if present.
            string authorExpression = message.Role == AuthorRole.User ? string.Empty : $" - {message.AuthorName ?? "*"}";
            // Include TextContent (via ChatMessageContent.Content), if present.
            string contentExpression = string.IsNullOrWhiteSpace(message.Content) ? string.Empty : message.Content;
            bool isCode = false;
            string codeMarker = isCode ? "\n  [CODE]\n" : " ";
            Console.WriteLine($"\n# {message.Role}{authorExpression}:{codeMarker}{contentExpression}");

            // Provide visibility for inner content (that isn't TextContent).
            foreach (KernelContent item in message.Items)
            {
                if (item is AnnotationContent annotation)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {annotation.Quote}: File #{annotation.FileId}");
                }
                else if (item is FileReferenceContent fileReference)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] File #{fileReference.FileId}");
                }
                else if (item is ImageContent image)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {image.Uri?.ToString() ?? image.DataUri ?? $"{image.Data?.Length} bytes"}");
                }
                else if (item is FunctionCallContent functionCall)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {functionCall.Id}");
                }
                else if (item is FunctionResultContent functionResult)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {functionResult.CallId}");
                }
            }

            if (message.Metadata?.TryGetValue("Usage", out object? usage) ?? false)
            {
                if (usage is RunStepTokenUsage assistantUsage)
                {
                    WriteUsage(assistantUsage.TotalTokenCount, assistantUsage.InputTokenCount, assistantUsage.OutputTokenCount);
                }
                else if (usage is ChatTokenUsage chatUsage)
                {
                    WriteUsage(chatUsage.TotalTokenCount, chatUsage.InputTokenCount, chatUsage.OutputTokenCount);
                }
            }

            void WriteUsage(int totalTokens, int inputTokens, int outputTokens)
            {
                Console.WriteLine($"  [Usage] Tokens: {totalTokens}, Input: {inputTokens}, Output: {outputTokens}");
            }
        }

        protected async Task DownloadResponseContentAsync(OpenAIFileClient client, Microsoft.SemanticKernel.ChatMessageContent message)
        {
            foreach (KernelContent item in message.Items)
            {
                if (item is AnnotationContent annotation)
                {
                    await this.DownloadFileContentAsync(client, annotation.FileId!);
                }
            }
        }

        protected async Task DownloadResponseImageAsync(OpenAIFileClient client, Microsoft.SemanticKernel.ChatMessageContent message)
        {
            foreach (KernelContent item in message.Items)
            {
                if (item is FileReferenceContent fileReference)
                {
                    await this.DownloadFileContentAsync(client, fileReference.FileId, launchViewer: true);
                }
            }
        }

        private async Task DownloadFileContentAsync(OpenAIFileClient client, string fileId, bool launchViewer = false)
        {
            OpenAIFile fileInfo = client.GetFile(fileId);
            if (fileInfo.Purpose == FilePurpose.AssistantsOutput)
            {
                string filePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileInfo.Filename));
                if (launchViewer)
                {
                    filePath = Path.ChangeExtension(filePath, ".png");
                }

                BinaryData content = await client.DownloadFileAsync(fileId);
                File.WriteAllBytes(filePath, content.ToArray());
                Console.WriteLine($"  File #{fileId} saved to: {filePath}");

                if (launchViewer)
                {
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/C start {filePath}"
                        });
                }
            }
        }
    
        public Kernel CreateKernelWithChatCompletion()
        {
            var builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(
                          "modelname",
                          "endpoint",
                          "apikey");
            return builder.Build();
        }

        /// <inheritdoc/>
        public override void WriteLine(object? value = null)
            => this.Output.WriteLine(value ?? string.Empty);

        /// <inheritdoc/>
        public override void WriteLine(string? format, params object?[] arg)
            => this.Output.WriteLine(format ?? string.Empty, arg);

        /// <inheritdoc/>
        public override void WriteLine(string? value)
            => this.Output.WriteLine(value ?? string.Empty);

        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="ITestOutputHelper"/> only supports output that ends with a newline.
        /// User this method will resolve in a call to <see cref="WriteLine(string?)"/>.
        /// </remarks>
        public override void Write(object? value = null)
            => this.Output.WriteLine(value ?? string.Empty);

        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="ITestOutputHelper"/> only supports output that ends with a newline.
        /// User this method will resolve in a call to <see cref="WriteLine(string?)"/>.
        /// </remarks>
        public override void Write(char[]? buffer)
            => this.Output.WriteLine(new string(buffer));

        /// <inheritdoc/>
        public override Encoding Encoding => Encoding.UTF8;

        /// <summary>
        /// Outputs the last message in the chat history.
        /// </summary>
        /// <param name="chatHistory">Chat history</param>
        protected void OutputLastMessage(ChatHistory chatHistory)
        {
            var message = chatHistory.Last();

            Console.WriteLine($"{message.Role}: {message.Content}");
            Console.WriteLine("------------------------");
        }

        /// <summary>
        /// Utility method to write a horizontal rule to the console.
        /// </summary>
        protected void WriteHorizontalRule()
            => Console.WriteLine(new string('-', HorizontalRuleLength));


        #region private
        private const int HorizontalRuleLength = 80;
        #endregion
    }

    public static class TextOutputHelperExtensions
    {
        public static void WriteLine(this ITestOutputHelper testOutputHelper, object target)
        {
            testOutputHelper.WriteLine(target.ToString());
        }

        public static void WriteLine(this ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.WriteLine(string.Empty);
        }

        public static void Write(this ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.WriteLine(string.Empty);
        }

        /// <summary>
        /// Current interface ITestOutputHelper does not have a Write method. This extension method adds it to make it analogous to Console.Write when used in Console apps.
        /// </summary>
        /// <param name="testOutputHelper">TestOutputHelper</param>
        /// <param name="target">Target object to write</param>
        public static void Write(this ITestOutputHelper testOutputHelper, object target)
        {
            testOutputHelper.WriteLine(target.ToString());
        }
    }
}