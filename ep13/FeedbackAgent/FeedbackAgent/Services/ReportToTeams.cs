using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace FeedbackAgent.Services
{
    public class ReportToTeams
    {
        public static async Task SendFeedback(Feedback feedback)
        {
            var id = feedback.AccountId;
            var email = !string.IsNullOrEmpty(feedback.EmailAddress) ? feedback.EmailAddress : "없음";

            var feedbackType = string.Empty;
            var detailType = string.Empty;
            if (feedback.FeedbackType == FeedbackType.Suggestion)
            {
                feedbackType = "피드백";

                switch (feedback.SuggestionType)
                {
                    case SuggestionType.Suggestion:
                        detailType = "😀있었으면 좋겠어요";
                        break;
                    case SuggestionType.Uncomfortable:
                        detailType = "🙁불편해요";
                        break;
                    default:
                        detailType = "🙁불편해요";
                        break;
                }
            }
            else
            {
                feedbackType = "버그";
                switch (feedback.BugSeverityType)
                {
                    case BugSeverityType.Low:
                        detailType = "🙂조금";
                        break;
                    case BugSeverityType.Normal:
                        detailType = "🙁보통";
                        break;
                    case BugSeverityType.High:
                        detailType = "😡매우";
                        break;
                    default:
                        detailType = "🙁보통";
                        break;
                }
            }

            var facts = new List<Fact>()
            {
                new Fact("Id:", feedback.Id),
                new Fact("타입:", feedbackType),
                new Fact("분류:",  detailType),
                new Fact("내용:", feedback.Content),
                new Fact("번역:", feedback.TranslatedContent),
                new Fact("언어:", feedback.Language),
                new Fact("코멘트:", feedback.Commentary),
                new Fact("리포트:", $"{Credential.WebAppUri}{feedback.Id}"),
            };

            if (!string.IsNullOrEmpty(email))
                facts.Add(new Fact("사용자이메일:", email));

            var sections = new List<Section>();
            var overviewSection = new Section()
            {
                text = "새로운 피드백이 있습니다.",
                facts = facts,
            };

            sections.Add(overviewSection);

            var actionableCard = new ActionableCard()
            {
                sections = sections
            };

            var httpClient = new HttpClient();
            await httpClient.PostAsJsonAsync(Credential.TeamsUri, actionableCard).ConfigureAwait(false);
        }

        public class Fact
        {
            public Fact() { }

            public Fact(string name, string value)
            {
                this.name = name;
                this.value = value;
            }

            public string name { get; set; }
            public string value { get; set; }
        }

        public class Section
        {
            public string activityTitle { get; set; }
            public string activitySubtitle { get; set; }
            public string activityImage { get; set; }
            public List<Fact> facts { get; set; }
            public string text { get; set; }
        }

        public class ActionableCard
        {
            public ActionableCard()
            {

            }

            public ActionableCard(string title) => this.title = title;

            public string? summary { get; set; } = "Actionable Card";
            public string? themeColor { get; set; } = "25AAE1";
            public string? title { get; set; }
            public List<Section>? sections { get; set; }
        }
    }
}
