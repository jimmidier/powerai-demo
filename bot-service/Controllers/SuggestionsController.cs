using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using TestTeamsApp.Helpers;
using System.Text.Json.Serialization;

namespace TestTeamsApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuggestionsController(IConfiguration configuration, IHostEnvironment hostEnvironment) : ControllerBase
{
    private readonly IConfiguration _configuration = configuration;

    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;

    public class SuggestionRequest
    {
        [JsonPropertyName("code")]
        public required string Code { get; set; }

        [JsonPropertyName("userIntent")]
        public string? UserIntent { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> GetSuggestions([FromBody] SuggestionRequest request)
    {
        if (string.IsNullOrEmpty(request.Code))
        {
            return BadRequest("Code is required");
        }

        var chatContext = CodeCacheHelper.GetContext(request.Code);
        if (chatContext == null)
        {
            if (!_hostEnvironment.IsProduction())
            {
                chatContext = new ChatContext(ChatContext.MockMessages, "Zeno Wang [SSW]", "确认一下代码变动", "Jim Zheng [SSW]");
            }
            else
            {
                return NotFound("No chat context found for the provided code");
            }
        }

        int suggestedReplyCount = 3;

        var baseUri = _configuration["API_SERVER_URL"];

        // var suggestionsStartTime = DateTime.Now;
        // Console.WriteLine($"开始获取建议回复: {suggestionsStartTime:yyyy-MM-dd HH:mm:ss.fff}");

        var suggestedReplies = await ChatMessageHelper.GetSuggestedRepliesAsync(
            baseUri!,
            chatContext,
            suggestedReplyCount,
            request.UserIntent);

        // var suggestionsEndTime = DateTime.Now;
        // Console.WriteLine($"获取建议回复结束: {suggestionsEndTime:yyyy-MM-dd HH:mm:ss.fff}");
        // Console.WriteLine($"获取建议回复总耗时: {(suggestionsEndTime - suggestionsStartTime).TotalMilliseconds} 毫秒");

        return Ok(suggestedReplies);
    }
}
