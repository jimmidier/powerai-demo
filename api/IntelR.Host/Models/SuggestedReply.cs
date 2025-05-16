namespace IntelR.Host.Models;

[Serializable]
public class SuggestedReply
{
    public List<SuggestedReplyItem> Suggestions { get; set; } = [];
    public List<SuggestedTopicItem> Topics { get; set; } = [];
}

[Serializable]
public class SuggestedReplyItem
{
    public int ReplyNumber { get; set; }
    public string Content { get; set; } = string.Empty;
}

[Serializable]
public class SuggestedTopicItem
{
    public string TopicName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}