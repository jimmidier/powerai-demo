using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Connector.Authentication;
using TestTeamsApp.Helpers;

namespace TestTeamsApp.Action;

public class ActionApp : TeamsActivityHandler
{
    protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
    {
        var tokenResponse = await GetTokenResponse(turnContext, action.State, cancellationToken);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
        {
            var signInLink = await GetSignInLinkAsync(turnContext, cancellationToken).ConfigureAwait(false);
            
            return new MessagingExtensionActionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "auth",
                    SuggestedActions = new MessagingExtensionSuggestedAction
                    {
                        Actions = new List<CardAction>
                                {
                                    new CardAction
                                    {
                                        Type = ActionTypes.OpenUrl,
                                        Value = signInLink,
                                        Title = "Bot Service OAuth",
                                    },
                                },
                    },
                },
            };
        }        
        int messageCount = 5;
        var client = new GraphClient(tokenResponse.Token);

        var chatMessages = await client.GetChatMessagesAsync(turnContext.Activity.Conversation.Id, cancellationToken, messageCount);
        
        string chatCode = CodeCacheHelper.GenerateCode(turnContext.Activity.Conversation.Id);
        CodeCacheHelper.StoreMessages(chatCode, chatMessages);

        var currentUser = await client.GetCurrentUserAsync(cancellationToken);
        
        Console.WriteLine($"生成的短代码: {chatCode}");
        Console.WriteLine($"当前用户: {currentUser.DisplayName}");
        Console.WriteLine($"Url: https://power-ai-front-end.vercel.app/?code={chatCode}&username={Uri.EscapeDataString(currentUser.DisplayName)}");
        return new MessagingExtensionActionResponse
        {
            Task = new TaskModuleContinueResponse
            {                
                Value = new TaskModuleTaskInfo
                {
                    Url = $"https://power-ai-front-end.vercel.app/?code={chatCode}&username={Uri.EscapeDataString(currentUser.DisplayName)}",
                    Height = 500,
                    Width = 600,
                    Title = "Power AI",
                },
            },
        };
    }

    private async Task<string> GetSignInLinkAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        var userTokenClient = turnContext.TurnState.Get<UserTokenClient>();
        var resource = await userTokenClient.GetSignInResourceAsync("BotTeamsAuthADv2", turnContext.Activity as Activity, null, cancellationToken).ConfigureAwait(false);
        return resource.SignInLink;
    }

    private async Task<TokenResponse> GetTokenResponse(ITurnContext<IInvokeActivity> turnContext, string state, CancellationToken cancellationToken)
    {
        var magicCode = string.Empty;

        if (!string.IsNullOrEmpty(state) && int.TryParse(state, out var parsed))
        {
            magicCode = parsed.ToString();
        }

        var userTokenClient = turnContext.TurnState.Get<UserTokenClient>();
        return await userTokenClient.GetUserTokenAsync(turnContext.Activity.From.Id, "BotTeamsAuthADv2", turnContext.Activity.ChannelId, magicCode, cancellationToken).ConfigureAwait(false);
    }
}