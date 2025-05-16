using System.Text.Json;
using IntelR.Host.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace IntelR.Host.Services;

public class AIService(
    KernelFactory kernelFactory,
    ChatHistoryProcessor chatHistoryProcessor,
    LlmResponseParser llmResponseParser,
    ILogger<AIService> logger)
{
    private readonly KernelFactory _kernelFactory = kernelFactory;
    private readonly ChatHistoryProcessor _chatHistoryProcessor = chatHistoryProcessor;
    private readonly LlmResponseParser _llmResponseParser = llmResponseParser;
    private readonly ILogger<AIService> _logger = logger;

    public async Task<SuggestedReply> GetSuggestedReplyAsync(GenerateReplyRequest request)
    {
        var kernel = await _kernelFactory.GetKernelAsync();

        var chatHistory = _chatHistoryProcessor.ConvertToOpenAIMessages(
            request,
            PromptTemplates.GitHubMcpPrompt
        );

        var githubIntentAnalysisUserPrompt = chatHistory.FirstOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;

        var githubIntentAnalysisResult = await kernel.InvokePromptAsync(githubIntentAnalysisUserPrompt, new(new OpenAIPromptExecutionSettings
        {
            ChatSystemPrompt = chatHistory.FirstOrDefault(m => m.Role == AuthorRole.System)?.Content,
            ResponseFormat = typeof(GitHubIntentAnalysis),
            FunctionChoiceBehavior = FunctionChoiceBehavior.None()
        }));

        _logger.LogDebug("githubIntentAnalysisResult: {GithubIntentAnalysisResult}", githubIntentAnalysisResult.ToString());

        var suggestedReplyUserPrompt = "Context for GitHub information: ";

        var githubIntentAnalysisResultJson = JsonSerializer.Deserialize<GitHubIntentAnalysis>(githubIntentAnalysisResult.ToString());
        if (githubIntentAnalysisResultJson != null
            && githubIntentAnalysisResultJson.IsGitHubRelated
            && !string.IsNullOrEmpty(githubIntentAnalysisResultJson.Prompt))
        {
            var mcpPromptResult = await kernel.InvokePromptAsync(
                githubIntentAnalysisResultJson.Prompt,
                new(new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                    ResponseFormat = typeof(GitHubMcpResponse),
                    Temperature = 0,
                    MaxTokens = 1500
                }));

            _logger.LogDebug("mcp query result: {McpPromptResult}", mcpPromptResult.ToString());

            var mcpPromptResultJson = JsonSerializer.Deserialize<GitHubMcpResponse>(mcpPromptResult.ToString());

            var isSuccessMcpRequest = mcpPromptResultJson?.IsSuccess ?? false;
            suggestedReplyUserPrompt += isSuccessMcpRequest
                ? mcpPromptResultJson!.Result
                : "(None)";
        }

        _logger.LogDebug("suggestedReplyUserPrompt: {SuggestedReplyUserPrompt}", suggestedReplyUserPrompt);

        var suggestedReplyPromptResult = await kernel.InvokePromptAsync(
            suggestedReplyUserPrompt,
            new(new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.None(),
                ChatSystemPrompt = PromptTemplates.SuggestedReplyBasePrompt,
                Temperature = 0,
                MaxTokens = 1500
            }));

        _logger.LogDebug("suggestedReplyPromptResult: {SuggestedReplyPromptResult}", suggestedReplyPromptResult.ToString());

        var suggestedReplies = _llmResponseParser.ParseSuggestedReply(suggestedReplyPromptResult.ToString(), request.SuggestedReplyCount);

        return suggestedReplies;
    }
}