namespace IntelR.Shared;

[Serializable]
public class SuggestedTopicsAndReplies
{
    public List<string> Suggestions { get; set; } = [];
    public List<SuggestedTopicItem> Topics { get; set; } = [];
}

[Serializable]
public class BasicSuggestedReply
{
    public string Content { get; set; } = string.Empty;
}

[Serializable]
public class SuggestedReply : BasicSuggestedReply
{
    public SuggestedReplyItemMetadata Metadata { get; set; } = new();

    public SuggestedReply() { }

    public SuggestedReply(BasicSuggestedReply basicReply)
    {
        Content = basicReply.Content;
    }
}

[Serializable]
public class SuggestedReplyItemMetadata
{
    public List<SuggestedReplyAction> Actions { get; set; } = [];
}

[Serializable]
public class SuggestedReplyAction
{
    public string Name { get; set; } = string.Empty;

    public SuggestedReplyActionType Type { get; set; }

    public string Url { get; set; } = string.Empty;
}

public enum SuggestedReplyActionType
{
    Link
}