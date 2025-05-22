using System.Text.Json;
using System.Text;
using TestTeamsApp.Models;

namespace TestTeamsApp.Helpers;

public static class LlmApiHelper
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

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
            return JsonSerializer.Deserialize<ApiResponse>(responseBody, _jsonSerializerOptions);
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
}