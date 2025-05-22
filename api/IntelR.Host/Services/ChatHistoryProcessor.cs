using IntelR.Shared;
using Microsoft.SemanticKernel.ChatCompletion;

namespace IntelR.Host.Services;

public class ChatHistoryProcessor
{
    public ChatHistory ConvertToOpenAIMessages(GenerateReplyRequest request, string systemPrompt)
    {
        var chatHistoryText = FormatChatHistory(request.ChatHistory);

        // Determine current user identity
        string userIdentity = !string.IsNullOrEmpty(request.CurrentUserName)
            ? $"You are responding as {request.CurrentUserName}. Match their professional style and role."
            : "You are responding on behalf of the user.";

        // Set reply count
        string replyCountInstruction = request.SuggestedReplyCount > 0
            ? $"Generate exactly {request.SuggestedReplyCount} distinct professional reply options."
            : "Generate 3 distinct professional reply options.";

        // Structure the intent section
        string intentSection = "";
        if (!string.IsNullOrEmpty(request.UserIntent))
        {
            intentSection = $"USER INTENT: {request.UserIntent}\n" +
                          "The above user intent must be your top priority when crafting replies.";
        }

        string targetSection = "";
        if (!string.IsNullOrEmpty(request.TargetUser) && !string.IsNullOrEmpty(request.TargetMessage))
        {
            targetSection = $"TARGET MESSAGE: Message from {request.TargetUser}: \"{request.TargetMessage}\"\n" +
                           "You should specifically reply to this target message. Analyze conversation history before this message to understand context and provide relevant responses.";
        }

        var userPrompt = $"CONTEXT: Teams workplace conversation\n\n" +
                       $"CURRENT USER: {userIdentity}\n\n" +
                       $"TASK: {replyCountInstruction} All replies must be in English regardless of the conversation language.\n\n" +
                       (!string.IsNullOrEmpty(intentSection) ? $"{intentSection}\n\n" : "") +
                       (!string.IsNullOrEmpty(targetSection) ? $"{targetSection}\n\n" : "") +
                       $"CONVERSATION HISTORY:\n{chatHistoryText}";

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage(userPrompt);

        return chatHistory;
    }

    private static string FormatChatHistory(List<UserChatMessage> chatHistory) =>
        string.Join("\n", chatHistory.Select(m => $"[{m.Timestamp}] {m.Sender}: {m.Content}"));
}