using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Graph.Models;

namespace TestTeamsApp.Helpers;

public static class ChatMessageHelper
{
    public static List<string> FormatChatMessages(IEnumerable<ChatMessage> messages)
    {
        var filteredMessages = messages
            .Where(m => m.MessageType?.ToString() == "Message")
            .OrderBy(m => m.CreatedDateTime)
            .ToList();
        
        var formattedMessages = new List<string>();
        
        foreach (var message in filteredMessages)
        {
            var displayName = message.From?.User?.DisplayName ?? "Unknown User";
            var plainText = StripHtmlTags(message.Body.Content);
            formattedMessages.Add($"{displayName}: {plainText}");
        }
        
        return formattedMessages;
    }
    
    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;
        
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

    public static async Task<List<string>> GetSuggestedRepliesAsync(string baseUrl, IEnumerable<ChatMessage> messages, string currentUserName, int suggestedReplyCount = 3, string? userIntent = null, double temperature = 0.7, int maxTokens = 800)
    {   
        var processStartTime = DateTime.Now;
        Console.WriteLine($"开始预处理聊天记录: {processStartTime:yyyy-MM-dd HH:mm:ss.fff}");
        var jsonContent = ConvertToJsonFormat(messages);
        var proposeEndTime = DateTime.Now;
        Console.WriteLine($"预处理聊天记录结束: {proposeEndTime:yyyy-MM-dd HH:mm:ss.fff}");
        Console.WriteLine($"预处理聊天记录总耗时: {(proposeEndTime - processStartTime).TotalMilliseconds} 毫秒");
        return await LlmApiHelper.GetSuggestedRepliesAsync(baseUrl, jsonContent, currentUserName, suggestedReplyCount, userIntent, temperature, maxTokens);
    }
}