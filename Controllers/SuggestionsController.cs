using Microsoft.AspNetCore.Mvc;
using TestTeamsApp.Helpers;

namespace TestTeamsApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuggestionsController : ControllerBase
{
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

        var suggestedReplies = await ChatMessageHelper.GetSuggestedRepliesAsync(chatMessages, userName, suggestedReplyCount);
        
        return new JsonResult(suggestedReplies, new System.Text.Json.JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}
