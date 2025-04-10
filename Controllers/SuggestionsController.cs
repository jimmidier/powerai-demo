using Microsoft.AspNetCore.Mvc;
using TestTeamsApp.Helpers;

namespace TestTeamsApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuggestionsController(IConfiguration configuration) : ControllerBase
{
    private readonly IConfiguration _configuration = configuration;

    [HttpGet]
    public async Task<IActionResult> GetSuggestions(string code, string userName)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(userName))
        {
            return BadRequest("Code and userName are required parameters");
        }

        userName = Uri.UnescapeDataString(userName);

        var chatMessages = CodeCacheHelper.GetMessages<dynamic>(code);
        if (chatMessages == null)
        {
            return NotFound("No chat messages found for the provided code");
        }        
        int suggestedReplyCount = 3;

        var baseUri = _configuration["API_SERVER_URL"];
        var suggestedReplies = await ChatMessageHelper.GetSuggestedRepliesAsync(baseUri, chatMessages, userName, suggestedReplyCount);
        
        return new JsonResult(suggestedReplies, new System.Text.Json.JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}
