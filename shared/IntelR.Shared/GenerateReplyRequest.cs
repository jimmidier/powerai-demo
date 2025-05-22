namespace IntelR.Shared;

[Serializable]
public class GenerateReplyRequest
{
    public string? Key{ get; set; }
    public string ChatId { get; set; } = string.Empty;
    public List<UserChatMessage> ChatHistory { get; set; } = [];
    public string CurrentUserName { get; set; } = string.Empty;
    public string TargetUser { get; set; } = string.Empty;
    public string TargetMessage { get; set; } = string.Empty;
    public string? UserIntent { get; set; }
    public int SuggestedReplyCount { get; set; } = 3;
    public ParameterOptions? Parameters { get; set; }

    public bool IsValid() => !string.IsNullOrEmpty(Key) || ChatHistory != null && ChatHistory.Count > 0;
}

[Serializable]
public class UserChatMessage
{
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
}

[Serializable]
public class ParameterOptions
{
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}