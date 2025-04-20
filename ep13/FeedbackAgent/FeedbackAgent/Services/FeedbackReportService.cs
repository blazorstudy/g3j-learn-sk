using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace FeedbackAgent.Services
{
    public class FeedbackReportService
    {
        public async Task<Feedback> Report(Feedback feedback)
        {

            var aiService = new AIService();
            var cosmosDb = new CosmosDbService("Feedback");

            feedback = await AIService.AnalyzeFeedback(feedback);
            await cosmosDb.Upsert(feedback);
            await ReportToTeams.SendFeedback(feedback);

            return feedback;
        }

        public async Task<Feedback?> Get(string id)
        {
            var cosmosDb = new CosmosDbService("Feedback");
            var feedback = await cosmosDb.Get<Feedback>(nameof(Feedback), id);

            return feedback;
        }
    }

    public class AnalyzedFeedbackResult
    {
        [Description("언어를 한국어로 표기해줘.")]
        public string? Language { get; set; }

        [Description("한국어로 번역한 문장")]
        public string? TranslatedContent { get; set; }

        [Description("Feedback을 분석해서 어떠한 피드백인지 한국어로 코멘트를 달아줘.")]
        public string? Commentary { get; set; }
    }
}
