using System.ComponentModel;
using System.Text.Json.Serialization;

public class Feedback : ICosmosModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FeedbackType FeedbackType { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SuggestionType SuggestionType { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BugSeverityType BugSeverityType { get; set; }

    public string? Content { get; set; }

    public string? EmailAddress { get; set; }

    public string? From { get; set; }

    public string? AccountId { get; set; }

    public string PartitionKey { get; set; } = nameof(Feedback);

    public Feedback()
    {
        Id = Guid.NewGuid().ToString();
    }

    public void Suggestion(Feedback feedback, FeedbackType feedbackType, SuggestionType suggestionType, string content, string email = "")
    {
        feedback.FeedbackType = feedbackType;
        feedback.SuggestionType = suggestionType;
        feedback.Content = content;
        feedback.EmailAddress = email;
    }

    public void Bug(Feedback feedback, FeedbackType feedbackType, BugSeverityType bugSeverityType, string content, string email = "")
    {
        feedback.FeedbackType = feedbackType;
        feedback.BugSeverityType = bugSeverityType;
        feedback.Content = content;
        feedback.EmailAddress = email;
    }


    public string? Language { get; set; }

    public string? TranslatedContent { get; set; }

    public string? Commentary { get; set; }
    
}

public enum FeedbackType { Suggestion, Bug }

public enum SuggestionType { Suggestion, Uncomfortable }

public enum BugSeverityType { Low, Normal, High }
