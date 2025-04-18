using System.Text.Json;
using System.Text;

namespace TestTeamsApp.Helpers;

public static class LlmApiHelper
{
    public static async Task<List<string>> GetSuggestedRepliesAsync(string baseUri, string chatHistoryJson, string currentUserName, int suggestedReplyCount = 3, string? UserIntent = null, double temperature = 0.7, int maxTokens = 800)
    {
        var requestObject = new
        {
            chatHistory = JsonSerializer.Deserialize<JsonElement>(chatHistoryJson).GetProperty("chatHistory"),
            currentUserName,
            suggestedReplyCount,
            UserIntent,
            parameters = new { temperature, maxTokens }
        };

        var jsonContent = JsonSerializer.Serialize(requestObject);
        using var httpClient = new HttpClient();
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(baseUri, content);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            return ParseSuggestedReplies(responseBody);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error calling PowerAI API: {ex.Message}");
            return new List<string> { $"Error getting reply suggestion: {ex.Message}" };
        }
    }

    private static List<string> ParseSuggestedReplies(string jsonResponse)
    {
        try
        {
            var suggestions = new List<string>();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

            if (jsonElement.TryGetProperty("Suggestions", out var suggestionsElement))
            {
                foreach (var suggestion in suggestionsElement.EnumerateArray())
                {
                    if (suggestion.TryGetProperty("Content", out var contentElement))
                    {
                        suggestions.Add(contentElement.GetString());
                    }
                }
            }

            return suggestions.Count > 0 ? suggestions : new List<string> { "No suggestions available." };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing API response: {ex.Message}");
            return new List<string> { $"Error parsing suggestions: {ex.Message}" };
        }
    }
}