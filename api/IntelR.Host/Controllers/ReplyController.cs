using System.Security.Cryptography;
using System.Text;
using IntelR.Host.Services;
using IntelR.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace IntelR.Host.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class ReplyController(AIService aiService, IMemoryCache memoryCache, ILogger<ReplyController> logger) : ControllerBase
{
    private readonly AIService _aiService = aiService;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<ReplyController> _logger = logger;

    [HttpPost("[action]")]
    public Task<string> RegisterConversationAsync(GenerateReplyRequest request)
    {
        var cacheKey = GetGenerateReplyRequestCacheKey(request);

        _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return request;
        });

        return Task.FromResult(cacheKey);
    }

    [HttpPost("[action]")]
    public async Task<SuggestedReply> GenerateReplyAsync(GenerateReplyRequest request)
    {
        if (!request.IsValid())
        {
            _logger.LogError("Invalid request: {Request}", request);
            throw new ArgumentException("Invalid request. Please provide a valid chat history.", nameof(request));
        }

        var requestToProcess = request;
        if (!string.IsNullOrEmpty(request.Key))
        {
            if (!_memoryCache.TryGetValue(request.Key, out requestToProcess) || requestToProcess ==  null)
            {
                _logger.LogWarning("Generate reply request cache miss for key: {Key}", request.Key);
                throw new KeyNotFoundException($"No generate reply request cache found for key: {request.Key}");
            }
        }

        return await _aiService.GetSuggestedReplyAsync(requestToProcess);
    }

    [HttpPost("[action]")]
    public async Task<SuggestedReply> GenerateTopicsAsync(string chatKey)
    {
        if (string.IsNullOrEmpty(chatKey) || !_memoryCache.TryGetValue<GenerateReplyRequest>(chatKey, out var request) || request ==  null)
        {
            _logger.LogWarning("Generate reply request cache miss for key: {Key}", chatKey);
            throw new KeyNotFoundException($"No generate reply request cache found for key: {chatKey}");
        }

        return await _aiService.GetSuggestedReplyAsync(request);
    }

    private static string GetGenerateReplyRequestCacheKey(GenerateReplyRequest request)
    {
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes($"{request.ChatId}{request.UserIntent}"));
        return Convert.ToBase64String(hashBytes).Replace("=", string.Empty);
    }
}
