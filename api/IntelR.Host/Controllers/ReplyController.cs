using IntelR.Host.Models;
using IntelR.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntelR.Host.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class ReplyController(AIService aiService, ILogger<ReplyController> logger) : ControllerBase
{
    private readonly AIService _aiService = aiService;
    private readonly ILogger<ReplyController> _logger = logger;

    [HttpPost("[action]")]
    public async Task<SuggestedReply> GenerateReplyAsync(GenerateReplyRequest request)
    {
        if (!request.IsValid())
        {
            _logger.LogError("Invalid request: {Request}", request);
            throw new ArgumentException("Invalid request. Please provide a valid chat history.", nameof(request));
        }

        return await _aiService.GetSuggestedReplyAsync(request);
    }
}
