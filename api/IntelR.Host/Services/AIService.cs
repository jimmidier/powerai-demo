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

    public async Task<SuggestedTopics> GenerateTopicsAsync(RegisterConversationRequest request)
    {
        var kernel = await _kernelFactory.GetKernelAsync();

        var gitHubIntentAnalysisChatHistory = _chatHistoryProcessor.ConvertToOpenAIMessages(
            request,
            PromptTemplates.GetGitHubMcpPrePrompt(_mcpOptions.GitHubServer.Identity, _mcpOptions.GitHubServer.AllowedRepositories)
        );

        var githubIntentAnalysisUserPrompt = gitHubIntentAnalysisChatHistory.FirstOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;
        var githubIntentAnalysisSystemPrompt = gitHubIntentAnalysisChatHistory.FirstOrDefault(m => m.Role == AuthorRole.System)?.Content;

        var githubIntentAnalysisResultModel = await InvokePromptAsync<GitHubIntentAnalysis>(
            kernel,
            githubIntentAnalysisUserPrompt,
            githubIntentAnalysisSystemPrompt,
            promptName: "GitHub Intent Analysis");

        var generateTopicsChatHistory = _chatHistoryProcessor.ConvertToOpenAIMessages(
            request,
            PromptTemplates.TopicBasePrompt
        );

        var generateTopicsUserPrompt = generateTopicsChatHistory.FirstOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;
        var generateTopicsSystemPrompt = generateTopicsChatHistory.FirstOrDefault(m => m.Role == AuthorRole.System)?.Content;

        generateTopicsUserPrompt += $"Analysis for GitHub intent: {githubIntentAnalysisResultModel.IsGitHubRelated}";

        var generateTopicsPromptResult = await InvokePromptAsync<SuggestedTopics>(
            kernel,
            generateTopicsUserPrompt,
            generateTopicsSystemPrompt,
            promptName: "Generate Topics");
        generateTopicsPromptResult.Topics.ForEach(t =>
        {
            if (t.GitHubIntentAnalysis.IsGitHubRelated)
            {
                t.GitHubIntentAnalysis.Prompt = githubIntentAnalysisResultModel.Prompt;
            }
        });

        return generateTopicsPromptResult;
    }

    public async Task<SuggestedReply> GenerateRepliesAsync(RegisterConversationRequest conversation, SuggestedTopicItem topic)
    {
        var kernel = await _kernelFactory.GetKernelAsync();

        string? mcpResult = null;

        if (topic.GitHubIntentAnalysis.IsGitHubRelated && !string.IsNullOrEmpty(topic.GitHubIntentAnalysis.Prompt))
        {
            var mcpPromptResult = await InvokePromptAsync<GitHubMcpResponse>(
                kernel,
                topic.GitHubIntentAnalysis.Prompt,
                PromptTemplates.GitHubMcpSystemPrompt,
                true,
                promptName: "GitHub MCP Query");

            if (mcpPromptResult?.IsSuccess ?? false)
            {
                mcpResult = mcpPromptResult!.Result;
            }
        }

        var suggestedReplyUserPrompt = mcpResult ?? "(None)";

        var chatHistory = _chatHistoryProcessor.ConvertToOpenAIMessages(
            conversation,
            PromptTemplates.SuggestedReplyBasePrompt
        );

        var userPrompt = chatHistory.FirstOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;
        var systemPrompt = chatHistory.FirstOrDefault(m => m.Role == AuthorRole.System)?.Content;
        userPrompt += $"""

            Topic: {topic.Name} - {topic.Summary}
            Context for GitHub information: {suggestedReplyUserPrompt}
        """;

        var reply = await InvokePromptAsync<BasicSuggestedReply>(
            kernel,
            userPrompt,
            systemPrompt,
            promptName: "Generate Suggested Reply");

        var result = new SuggestedReply(reply);
        if (topic.GitHubIntentAnalysis.IsGitHubRelated)
        {
            // TODO: This is only for test purpose
            result.Metadata.Actions.Add(new SuggestedReplyAction
            {
                Name = "YakShaver",
                Type = SuggestedReplyActionType.Link,
                Url = "https://yakshaver.com/api/testonly"
            });
            
            result.Metadata.Actions.Add(new SuggestedReplyAction
            {
                Name = "Email",
                Type = SuggestedReplyActionType.Link,
                Url = "mailto:info@example.com?subject=Test%20Only&body=Test%20message"
            });
        }

        return result;
    }

    public async Task<SuggestedTopicsAndReplies> GenerateTopicAndRepliesAsync(RegisterConversationRequest request)
    {
        var kernel = await _kernelFactory.GetKernelAsync();

        var gitHubIntentAnalysisChatHistory = _chatHistoryProcessor.ConvertToOpenAIMessages(
            request,
            PromptTemplates.GetGitHubMcpPrePrompt(_mcpOptions.GitHubServer.Identity, _mcpOptions.GitHubServer.AllowedRepositories)
        );

        var githubIntentAnalysisUserPrompt = gitHubIntentAnalysisChatHistory.FirstOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;
        var githubIntentAnalysisSystemPrompt = gitHubIntentAnalysisChatHistory.FirstOrDefault(m => m.Role == AuthorRole.System)?.Content;

        var githubIntentAnalysisResultModel = await InvokePromptAsync<GitHubIntentAnalysis>(
            kernel,
            githubIntentAnalysisUserPrompt,
            githubIntentAnalysisSystemPrompt);

        var suggestedReplyUserPrompt = "Context for GitHub information: ";
        string? mcpResult = null;

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
            PromptTemplates.TopicAndSuggestedReplyBasePrompt
        );

        suggestedReplyUserPrompt += suggestedReplyChatHistory.FirstOrDefault(m => m.Role == AuthorRole.User)?.Content ?? string.Empty;
        var suggestedReplySystemPrompt = suggestedReplyChatHistory.FirstOrDefault(m => m.Role == AuthorRole.System)?.Content;

        var suggestedReplyPromptResult = await kernel.InvokePromptAsync(
            suggestedReplyUserPrompt,
            new(new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.None(),
                ChatSystemPrompt = suggestedReplySystemPrompt,
                ResponseFormat = typeof(SuggestedTopicsAndReplies),
                MaxTokens = 1500
            }));

        var suggestedReplyResultModel = JsonSerializer.Deserialize<SuggestedTopicsAndReplies>(suggestedReplyPromptResult.ToString());

        return suggestedReplyResultModel ?? new();
    }

    private async Task<T> InvokePromptAsync<T>(
        Kernel kernel,
        string userPrompt,
        string? systemPrompt,
        bool autoFunctionChoice = false,
        int maxTokens = 1500,
        string? promptName = null)
    {
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var kernelArguments = new KernelArguments(new OpenAIPromptExecutionSettings
        {
            ChatSystemPrompt = systemPrompt,
            ResponseFormat = typeof(T),
            FunctionChoiceBehavior = autoFunctionChoice
                ? FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
                : FunctionChoiceBehavior.None(),
            MaxTokens = maxTokens
        });
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var result = (await kernel.InvokePromptAsync(
            userPrompt,
            kernelArguments))
            .ToString();

        promptName ??= typeof(T).Name;
        _logger.LogInformation("Prompt [{PromptName}] invocation result: {Result}", promptName, result.ToString());

        return JsonSerializer.Deserialize<T>(result.ToString()) ?? default!;
    }
}