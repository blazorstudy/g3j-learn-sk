using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.VisualBasic;
using System.Text.Json;

namespace FeedbackAgent.Services
{
    public class AIService
    {
        public static Kernel CreateKernel()
        {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion("gpt-4.1", Credential.EndPoint, Credential.ApiKey);

            var kernel = builder.Build();
            return kernel;
        }

        public static async Task<Feedback> AnalyzeFeedback(Feedback feedback)
        {
            var kernel = CreateKernel();

            var userGuideService = new UserGuideService();

            var searchResults = await userGuideService.Search(feedback.Content);

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = typeof(AnalyzedFeedbackResult)
            };

            var response = await kernel.InvokePromptAsync(
                $"""
                [Feedback]
                {JsonSerializer.Serialize(feedback, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })}

                [Guide Search Result]
                {JsonSerializer.Serialize(searchResults, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })}

                [Instruction]
                - Feedback을 한국어로 번역해줘.
                - Feedback이 무슨 언어로 되어 있는 지 표시해줘.
                - [Guide Search Result]에서 참조할 수 있는 것이 있다면 참조하고, Feedback을 분석해서 유저에게 어느 방향으로 안내를 하면 좋은 지 설명해줘.
                """, new(executionSettings));

            var result = JsonSerializer.Deserialize<AnalyzedFeedbackResult>(response.ToString());

            feedback.Language = result?.Language;
            feedback.TranslatedContent = result?.TranslatedContent;
            feedback.Commentary = result?.Commentary;

            return feedback;
        }
    }
}
