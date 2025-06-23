namespace IntelR.Shared;

[Serializable]
public class SuggestedTopicsAndReplies
{
    public List<string> Suggestions { get; set; } = [];
    public List<SuggestedTopicItem> Topics { get; set; } = [];
}

[Serializable]
public class SuggestedReplies
{
    public List<SuggestedReplyItem> Replies { get; set; } = [];

    public SuggestedReplies() { }

    public SuggestedReplies(List<SuggestedReplyItem> replies)
    {
        Replies = replies ?? [];
    }
}

[Serializable]
public class PromptSuggestedReplyItem
{
    public string Summary { get; set; } = string.Empty;
    public List<string> SuggestedActions { get; set; } = [];
    public string Content { get; set; } = string.Empty;
}

[Serializable]
public class SuggestedReplyItem
{
    public SuggestedReplyItemMetadata Metadata { get; set; } = new();

    public string Content { get; set; } = string.Empty;

    public SuggestedReplyItem() { }

    public SuggestedReplyItem(PromptSuggestedReplyItem promptReply)
    {
        Metadata = new SuggestedReplyItemMetadata
        {
            Summary = promptReply.Summary
        };
        Content = promptReply.Content;
    }
}

[Serializable]
public class SuggestedReplyItemMetadata
{
    public string Summary { get; set; } = string.Empty;
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