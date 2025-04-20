using Markdig;

public class ChatMessage
{
    public string Id { get; set; }

    public string? Message { get; set; }

    public ChatMessageRole Role { get; set; }

    public ChatMessage(ChatMessageRole role, string? message = null)
    {
        Id = Guid.NewGuid().ToString();
        Role = role;
        Message = message;
    }

    public string Username => Role == ChatMessageRole.User ? "사용자" : "💬 Feedback Agent";

    public string CSS
    {
        get
        {
            switch (Role)
            {
                case ChatMessageRole.Assistant:
                    return "received";
                case ChatMessageRole.User:
                    return "sent";
                case ChatMessageRole.System:
                default:
                    return "system";
            }
        }
    }

    #region 4. 마크다운 표시

    public string MarkdownMessage
    {
        get
        {
            var message = Message;
            if (!string.IsNullOrEmpty(message))
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                return Markdown.ToHtml(message);
            }

            return message;
        }
    }

    #endregion
}

public enum ChatMessageRole { System, User, Assistant }