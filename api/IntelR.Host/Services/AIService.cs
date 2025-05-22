using System.Text.Json;
using IntelR.Host.Models;
using IntelR.Shared;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace IntelR.Host.Services;

public class AIService(
    KernelFactory kernelFactory,
    ChatHistoryProcessor chatHistoryProcessor,
    IOptions<McpOptions> mcpOptions,
    ILogger<AIService> logger)
{
    private readonly KernelFactory _kernelFactory = kernelFactory;
    private readonly ChatHistoryProcessor _chatHistoryProcessor = chatHistoryProcessor;
    private readonly McpOptions _mcpOptions = mcpOptions.Value;
    private readonly ILogger<AIService> _logger = logger;

    public async Task<SuggestedReply> GetSuggestedReplyAsync(GenerateReplyRequest request)
    {
        var kernel = await _kernelFactory.GetKernelAsync();

        var gitHubIntentAnalysisChatHistory = _chatHistoryProcessor.ConvertToOpenAIMessages(
            request,
            PromptTemplates.GetGitHubMcpPrePrompt(_mcpOptions.GitHubServer.Identity, _mcpOptions.GitHubServer.AllowedRepositories)
        );

        var githubIntentAnalysisUserPrompt = gitHubIntentAnalysisChatHistory.FirstOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;
        var githubIntentAnalysisSystemPrompt = gitHubIntentAnalysisChatHistory.FirstOrDefault(m => m.Role == AuthorRole.System)?.Content;

        var githubIntentAnalysisKernelArguments = new KernelArguments(new OpenAIPromptExecutionSettings
        {
            ChatSystemPrompt = githubIntentAnalysisSystemPrompt,
            ResponseFormat = typeof(GitHubIntentAnalysis),
            FunctionChoiceBehavior = FunctionChoiceBehavior.None()
        });

        var githubIntentAnalysisResult = (await kernel.InvokePromptAsync(
            githubIntentAnalysisUserPrompt,
            githubIntentAnalysisKernelArguments))
            .ToString();

        _logger.LogDebug("githubIntentAnalysisResult: {GithubIntentAnalysisResult}", githubIntentAnalysisResult);

        var suggestedReplyUserPrompt = "Context for GitHub information: ";
        string? mcpResult = null;

        var githubIntentAnalysisResultModel = JsonSerializer.Deserialize<GitHubIntentAnalysis>(githubIntentAnalysisResult.ToString());
        if (githubIntentAnalysisResultModel != null
            && githubIntentAnalysisResultModel.IsGitHubRelated
            && !string.IsNullOrEmpty(githubIntentAnalysisResultModel.Prompt))
        {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var mcpPromptResult = await kernel.InvokePromptAsync(
                githubIntentAnalysisResultModel.Prompt,
                new(new OpenAIPromptExecutionSettings
                {
                    ChatSystemPrompt = PromptTemplates.GitHubMcpSystemPrompt,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true }),
                    ResponseFormat = typeof(GitHubMcpResponse),
                    Temperature = 0,
                    MaxTokens = 1500
                }));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            _logger.LogDebug("mcp query result: {McpPromptResult}", mcpPromptResult.ToString());

            var mcpPromptResultModel = JsonSerializer.Deserialize<GitHubMcpResponse>(mcpPromptResult.ToString());

            if (mcpPromptResultModel?.IsSuccess ?? false)
            {
                mcpResult = mcpPromptResultModel!.Result;
            }
        }

        suggestedReplyUserPrompt += mcpResult ?? "(None)";

        _logger.LogDebug("suggestedReplyUserPrompt: {SuggestedReplyUserPrompt}", suggestedReplyUserPrompt);

        var suggestedReplyChatHistory = _chatHistoryProcessor.ConvertToOpenAIMessages(
            request,
            PromptTemplates.SuggestedReplyBasePrompt
        );

        suggestedReplyUserPrompt += suggestedReplyChatHistory.FirstOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;
        var suggestedReplySystemPrompt = suggestedReplyChatHistory.FirstOrDefault(m => m.Role == AuthorRole.System)?.Content;

        var suggestedReplyPromptResult = await kernel.InvokePromptAsync(
            suggestedReplyUserPrompt,
            new(new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.None(),
                ChatSystemPrompt = suggestedReplySystemPrompt,
                ResponseFormat = typeof(SuggestedReply),
                Temperature = 0,
                MaxTokens = 1500
            }));

        var suggestedReplyResultModel = JsonSerializer.Deserialize<SuggestedReply>(suggestedReplyPromptResult.ToString());

        return suggestedReplyResultModel ?? new();
    }
}