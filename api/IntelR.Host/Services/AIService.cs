using System.Text.Json;
using IntelR.Host.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace IntelR.Host.Services;

public class AIService(KernelFactory kernelFactory, ChatHistoryProcessor chatHistoryProcessor, LlmResponseParser llmResponseParser)
{
    private readonly KernelFactory _kernelFactory = kernelFactory;
    private readonly ChatHistoryProcessor _chatHistoryProcessor = chatHistoryProcessor;
    private readonly LlmResponseParser _llmResponseParser = llmResponseParser;

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

        Console.WriteLine($"githubIntentAnalysisResult: {githubIntentAnalysisResult}");

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

            Console.WriteLine($"mcp query result: {mcpPromptResult}");

            var mcpPromptResultJson = JsonSerializer.Deserialize<GitHubMcpResponse>(mcpPromptResult.ToString());
            if (mcpPromptResultJson?.IsSuccess ?? false)
            {
                suggestedReplyUserPrompt += mcpPromptResultJson.Result;
            }
        }

        var suggestedReplyPromptResult = await kernel.InvokePromptAsync(
            suggestedReplyUserPrompt,
            new(new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.None(),
                ChatSystemPrompt = PromptTemplates.SuggestedReplyBasePrompt,
                Temperature = 0,
                MaxTokens = 1500
            }));

        var suggestedReplies = _llmResponseParser.ParseSuggestedReply(suggestedReplyPromptResult.ToString(), request.SuggestedReplyCount);

        return suggestedReplies;
    }
}