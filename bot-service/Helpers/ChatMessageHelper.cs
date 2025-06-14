using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Graph.Models;
using TestTeamsApp.Models;

namespace TestTeamsApp.Helpers;

public static class ChatMessageHelper
{
    public static string StripHtmlTags(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }
        
        var plainText = Regex.Replace(html, "<[^>]*>", string.Empty);
        plainText = Regex.Replace(plainText, "&nbsp;", " ");
        plainText = Regex.Replace(plainText, "&amp;", "&");
        plainText = Regex.Replace(plainText, "&lt;", "<");
        plainText = Regex.Replace(plainText, "&gt;", ">");
        return plainText.Trim();
    }

    public static string ConvertToJsonFormat(IEnumerable<ChatMessage> messages)
    {
        var filteredMessages = messages
            .Where(m => m.MessageType?.ToString() == "Message")
            .OrderBy(m => m.CreatedDateTime)
            .ToList();

        var recentMessages = filteredMessages.ToList();

        var chatHistory = recentMessages.Select(m => new
        {
            sender = m.From?.User?.DisplayName ?? "Unknown User",
            content = StripHtmlTags(m.Body.Content),
            timestamp = m.CreatedDateTime?.ToString("yyyy-MM-ddTHH:mm:ssZ")
        }).ToList();

        var resultObject = new { chatHistory };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(resultObject, options);
    }

    public static async Task<ApiResponse> GetSuggestedRepliesAsync(string baseUrl, ChatContext context, int suggestedReplyCount = 3, string? userIntent = null, double temperature = 0.7, int maxTokens = 800)
    {
        var chatMessages = context.Messages;
        var currentUserName = context.CurrentUser;
        var targetUserName = context.TargetUser;
        var targetMessage = context.TargetMessage;

        var jsonContent = ConvertToJsonFormat(chatMessages);

        return await LlmApiHelper.GetSuggestedRepliesAsync(
            baseUrl,
            jsonContent,
            currentUserName,
            targetUserName,
            targetMessage,
            userIntent,
            suggestedReplyCount,
            temperature,
            maxTokens);
    }
}