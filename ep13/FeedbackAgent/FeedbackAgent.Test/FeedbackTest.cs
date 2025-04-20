using Azure;
using FeedbackAgent.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using System.Net;
using System.Text.Json;
using Xunit.Abstractions;

namespace FeedbackAgent.Test
{
    public class FeedbackTest
    {
        ITestOutputHelper output;
        public FeedbackTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async void Feedback_분석()
        {
            var feedback = new Feedback()
            {
                Content = "Ich habe aus Versehen auf die Premium-Testversion geklickt – wird sie nach Ablauf des Testzeitraums automatisch kostenpflichtig verlängert?",
                FeedbackType = FeedbackType.Bug,
                BugSeverityType = BugSeverityType.Normal,
            };

            feedback = await AIService.AnalyzeFeedback(feedback);

            output.WriteLine($"언어 : {feedback.Language}" );
            output.WriteLine($"번역 : {feedback.TranslatedContent}");
            output.WriteLine($"코멘트 : {feedback.Commentary}");
        }

        [Fact]
        public async void Feedback_Report()
        {
            var feedbackReportService = new FeedbackReportService();

            var feedback = new Feedback()
            {
                Content = "Ich habe aus Versehen auf die Premium-Testversion geklickt – wird sie nach Ablauf des Testzeitraums automatisch kostenpflichtig verlängert?",
                FeedbackType = FeedbackType.Bug,
                BugSeverityType = BugSeverityType.Normal,
                EmailAddress = "aa@aa.com"
            };

            await feedbackReportService.Report(feedback);

            var getFeedback = await feedbackReportService.Get(feedback.Id);

            output.WriteLine(getFeedback.Content);
        }

        [Fact]
        public async void EmbeddingGuide()
        {
            var service = new UserGuideService();

            var guide = new UserGuide()
            {
                Id = "1",
                Title = "체험 기간이 끝나면 자동으로 유료로 전환되나요?",
                Guide = "아니요. 체험 기간이 끝나더라도 자동으로 유로로 전환되지 않습니다. 체험 기간이 끝나더라도 별도로 결제하지 않는 이상 결제가 되지 않으니 안심하셔도 됩니다.",
            };

            await service.Upsert(guide);

            var result = await service.Search("체험 기간 끝나면 유료 전환");
        }
    }
}