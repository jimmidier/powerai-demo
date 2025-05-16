using System.Text.Json;
using System.Text;
using TestTeamsApp.Models;

namespace TestTeamsApp.Helpers;

public static class LlmApiHelper
{
    public static async Task<ApiResponse> GetSuggestedRepliesAsync(string baseUri, string chatHistoryJson, string currentUserName, string targetUser, string targetMessage, string? UserIntent = null, int suggestedReplyCount = 3, double temperature = 0.7, int maxTokens = 800)
    {
        var requestObject = new
        {
            chatHistory = JsonSerializer.Deserialize<JsonElement>(chatHistoryJson).GetProperty("chatHistory"),
            currentUserName,
            targetUser,
            targetMessage,
            UserIntent,
            suggestedReplyCount,
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
            return ParseApiResponse(responseBody);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error calling PowerAI API: {ex.Message}");
            return new ApiResponse
            {
                Suggestions = new List<string> { $"Error getting reply suggestion: {ex.Message}" }
            };
        }
    }

    private static ApiResponse ParseApiResponse(string jsonResponse)
    {
        var result = new ApiResponse();

        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

            if (jsonElement.TryGetProperty("Suggestions", out var suggestionsElement))
            {
                foreach (var suggestion in suggestionsElement.EnumerateArray())
                {
                    if (suggestion.TryGetProperty("Content", out var contentElement))
                    {
                        var content = contentElement.GetString();
                        if (content != null)
                        {
                            result.Suggestions.Add(content);
                        }
                    }
                }
            }

            if (jsonElement.TryGetProperty("Topics", out var topicsElement))
            {
                foreach (var topic in topicsElement.EnumerateArray())
                {
                    var topicInfo = new TopicInfo();

                    if (topic.TryGetProperty("TopicName", out var nameElement))
                    {
                        topicInfo.Name = nameElement.GetString() ?? string.Empty;
                    }

                    if (topic.TryGetProperty("Summary", out var summaryElement))
                    {
                        topicInfo.Summary = summaryElement.GetString() ?? string.Empty;
                    }

                    result.Topics.Add(topicInfo);
                }
            }

            if (result.Suggestions.Count == 0)
            {
                result.Suggestions.Add("No suggestions available.");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing API response: {ex.Message}");
            return new ApiResponse
            {
                Suggestions = new List<string> { $"Error parsing suggestions: {ex.Message}" }
            };
        }
    }
}