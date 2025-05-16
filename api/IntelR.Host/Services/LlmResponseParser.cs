using System.Text.RegularExpressions;
using IntelR.Host.Models;

namespace IntelR.Host.Services;

public class LlmResponseParser
{
    public SuggestedReply ParseSuggestedReply(string responseContent, int suggestedReplyCount = 3)
    {
        var topicsAndReplies = SplitTopicsAndReplies(responseContent);

        return new SuggestedReply
        {
            Suggestions = ExtractSuggestedReplies(topicsAndReplies.Item2, suggestedReplyCount),
            Topics = ExtractTopics(topicsAndReplies.Item1)
        };
    }

    private static (string, string) SplitTopicsAndReplies(string generatedText)
    {
        var topicsSection = "";
        
        var topicsMatch = Regex.Match(generatedText, @"===\s*TOPICS\s*===(.*?)(?:===\s*REPLIES\s*===|$)", RegexOptions.Singleline);
        var repliesMatch = Regex.Match(generatedText, @"===\s*REPLIES\s*===(.*?)$", RegexOptions.Singleline);

        if (topicsMatch.Success)
        {
            topicsSection = topicsMatch.Groups[1].Value.Trim();
        }

        string? repliesSection;
        if (repliesMatch.Success)
        {
            repliesSection = repliesMatch.Groups[1].Value.Trim();
        }
        else
        {
            repliesSection = generatedText;
        }

        return (topicsSection, repliesSection);
    }

    private List<SuggestedTopicItem> ExtractTopics(string topicsSection)
    {
        var topics = new List<SuggestedTopicItem>();
        if (string.IsNullOrWhiteSpace(topicsSection))
        {
            return topics;
        }

        var pattern = @"TOPIC\s*\d+\s*:\s*(.*?)\s*\|\s*(.*?)(?:\r?\n|$)";
        var matches = Regex.Matches(topicsSection, pattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            topics.Add(new SuggestedTopicItem
            {
                TopicName = match.Groups[1].Value.Trim(),
                Summary = match.Groups[2].Value.Trim()
            });
        }

        return topics;
    }

    private static List<SuggestedReplyItem> ExtractSuggestedReplies(string generatedText, int expectedCount = 3)
    {
        var suggestions = new List<SuggestedReplyItem>();
        var pattern = @"(\d+)\.\s+(.+?)(?=\n\d+\.|\n*$)";
        var matches = Regex.Matches(generatedText, pattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int replyNumber) && replyNumber >= 1)
            {
                suggestions.Add(new SuggestedReplyItem
                {
                    ReplyNumber = replyNumber,
                    Content = match.Groups[2].Value.Trim()
                });
            }
        }

        while (suggestions.Count < expectedCount)
        {
            suggestions.Add(new SuggestedReplyItem
            {
                ReplyNumber = suggestions.Count + 1,
                Content = "Sorry, I couldn't generate additional reply suggestions."
            });
        }

        return suggestions;
    }
}