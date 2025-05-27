using System.Security.Cryptography;
using System.Text;
using IntelR.Host.Services;
using IntelR.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace IntelR.Host.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class ReplyController(AIService aiService, IMemoryCache memoryCache, IOptions<ApplicationOptions> appOptions, ILogger<ReplyController> logger) : ControllerBase
{
    private readonly AIService _aiService = aiService;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ApplicationOptions _appOptions = appOptions.Value;
    private readonly ILogger<ReplyController> _logger = logger;

    [HttpPost("[action]")]
    public Task<string> RegisterConversationAsync(RegisterConversationRequest request)
    {
        var cacheKey = GetRegisterConversationRequestCacheKey(request);

        _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_appOptions.ConversationCachePeriodMinutes);
            return request;
        });

        return Task.FromResult(cacheKey);
    }

    [HttpPost("[action]")]
    public async Task<SuggestedTopicsAndReplies> GenerateReplyAsync(RegisterConversationRequest request)
    {
        if (!request.IsValid())
        {
            _logger.LogError("Invalid request: {Request}", request);
            throw new ArgumentException("Invalid request. Please provide a valid chat history.", nameof(request));
        }

        var requestToProcess = request;
        if (!string.IsNullOrEmpty(request.Key))
        {
            if (!_memoryCache.TryGetValue(request.Key, out requestToProcess) || requestToProcess == null)
            {
                _logger.LogWarning("Conversation cache miss for chat key: {Key}", request.Key);
                throw new KeyNotFoundException($"No conversation cache found for chat key: {request.Key}");
            }
        }

        return await _aiService.GenerateTopicAndRepliesAsync(requestToProcess);
    }

    [HttpPost("[action]")]
    public async Task<SuggestedTopics> GenerateTopicsAsync(GenerateTopicsRequest request)
    {
        var conversation = GetConversationFromCache(request.ChatKey);

        var suggestedTopics = await _aiService.GenerateTopicsAsync(conversation);
        var cacheKey = GetSuggestedTopicsCacheKey(conversation);

        _memoryCache.Set(cacheKey, suggestedTopics, TimeSpan.FromMinutes(_appOptions.ConversationCachePeriodMinutes));

        return suggestedTopics;
    }

    [HttpPost("[action]")]
    public async Task<SuggestedReply> GenerateRepliesAsync(GenerateReplyRequest request)
    {
        var (conversation, topics) = GetConversationTopicsFromCache(request.ChatKey);
        if (request.UseUserIntent)
        {
            conversation.UserIntent = request.UserIntent;
        }

        var requestTopic = topics.Topics.FirstOrDefault(t => t.Name.Equals(request.TopicName, StringComparison.OrdinalIgnoreCase));
        if (requestTopic == null)
        {
            _logger.LogWarning("Topic not found: {TopicName}", request.TopicName);
            throw new ArgumentException($"Speficied topic name is invalid: {request.TopicName}.", nameof(request));
        }

        return await _aiService.GenerateRepliesAsync(conversation, requestTopic);
    }

    private RegisterConversationRequest GetConversationFromCache(string chatKey)
    {
        if (string.IsNullOrEmpty(chatKey)
            || !_memoryCache.TryGetValue<RegisterConversationRequest>(chatKey, out var conversation)
            || conversation == null)
        {
            _logger.LogWarning("Conversation miss for chat key: {Key}", chatKey);
            throw new KeyNotFoundException($"No conversation found for chat key: {chatKey}");
        }

        return conversation;
    }

    private (RegisterConversationRequest conversation, SuggestedTopics topics) GetConversationTopicsFromCache(string chatKey)
    {
        var conversation = GetConversationFromCache(chatKey);

        var suggestedTopicsCacheKey = GetSuggestedTopicsCacheKey(conversation);
        if (!_memoryCache.TryGetValue<SuggestedTopics>(suggestedTopicsCacheKey, out var topics) || topics == null)
        {
            _logger.LogWarning("Topics cache miss for chat key: {Key}", chatKey);
            throw new KeyNotFoundException($"No topics cache found for chat key: {chatKey}");
        }

        return (conversation, topics);
    }

    private static string GetRegisterConversationRequestCacheKey(RegisterConversationRequest request)
    {
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes($"{nameof(RegisterConversationRequest)}-{request.GetUniqueKey()}"));
        return Convert.ToBase64String(hashBytes).Replace("=", string.Empty);
    }

    private static string GetSuggestedTopicsCacheKey(RegisterConversationRequest request)
    {
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes($"{nameof(SuggestedTopics)}-{request.GetUniqueKey()}"));
        return Convert.ToBase64String(hashBytes).Replace("=", string.Empty);
    }
}
