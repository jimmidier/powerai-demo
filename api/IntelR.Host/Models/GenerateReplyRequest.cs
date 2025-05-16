namespace IntelR.Host.Models;

[Serializable]
public class GenerateReplyRequest
{
    public List<UserChatMessage> ChatHistory { get; set; } = new();
    public string CurrentUserName { get; set; } = string.Empty;
    public string TargetUser { get; set; } = string.Empty;
    public string TargetMessage { get; set; } = string.Empty;

    public string UserIntent { get; set; } = string.Empty;
    public int SuggestedReplyCount { get; set; } = 3;
    public ParameterOptions? Parameters { get; set; }

    public bool IsValid() => ChatHistory != null && ChatHistory.Count > 0;
}

[Serializable]
public class UserChatMessage
{
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}

[Serializable]
public class ParameterOptions
{
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}