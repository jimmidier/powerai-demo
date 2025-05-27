namespace IntelR.Host;

public class ApplicationOptions
{
    private const int DefaultConversationCachePeriodMinutes = 5;

    public int ConversationCachePeriodMinutes { get; set; } = DefaultConversationCachePeriodMinutes;
}