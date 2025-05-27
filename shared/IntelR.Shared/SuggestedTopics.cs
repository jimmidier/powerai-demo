namespace IntelR.Shared;

[Serializable]
public class SuggestedTopics
{
    public List<SuggestedTopicItem> Topics { get; set; } = [];
}

[Serializable]
public class SuggestedTopicItem
{
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public GitHubIntentAnalysis GitHubIntentAnalysis { get; set; } = new();
}