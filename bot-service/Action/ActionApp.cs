using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Connector.Authentication;
using TestTeamsApp.Helpers;
using TestTeamsApp.RemoteApis;
using IntelR.Shared;

namespace TestTeamsApp.Action;

public class ActionApp(IIntelRApi intelRApi, IConfiguration configuration) : TeamsActivityHandler
{
    private readonly IIntelRApi _intelRApi = intelRApi;
    private readonly IConfiguration _configuration = configuration;

    protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(
        ITurnContext<IInvokeActivity> turnContext,
        MessagingExtensionAction action,
        CancellationToken cancellationToken)
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
                        Actions =
                        [
                            new CardAction
                            {
                                Type = ActionTypes.OpenUrl,
                                Value = signInLink,
                                Title = "Bot Service OAuth",
                            },
                        ],
                    },
                },
            };
        }

        int messageCount = 20;

        var client = new GraphClient(tokenResponse.Token);

        var chatMessages = await client.GetChatMessagesAsync(turnContext.Activity.Conversation.Id, messageCount, cancellationToken);

        var targetMessage = "";
        var targetUser = "";
        if (action.MessagePayload != null)
        {
            targetMessage = ChatMessageHelper.StripHtmlTags(action.MessagePayload.Body.Content ?? "");
            targetUser = action.MessagePayload.From.User.DisplayName ?? "";
        }

        var currentUser = await client.GetCurrentUserAsync(cancellationToken);
        var userName = currentUser.DisplayName ?? "";

        var generateReplyRequest = new GenerateReplyRequest
        {
            ChatId = turnContext.Activity.Conversation.Id,
            ChatHistory = [.. chatMessages.Select(m => new UserChatMessage
            {
                Sender = m.From?.User?.DisplayName ?? "Unknown User",
                Content = ChatMessageHelper.StripHtmlTags(m.Body?.Content),
                Timestamp = m.CreatedDateTime
            })],
            CurrentUserName = userName,
            TargetUser = targetUser,
            TargetMessage = targetMessage,
        };

        var chatCode = await _intelRApi.RegisterConversationAsync(generateReplyRequest);

        var frontendUrl = _configuration["FRONTEND_URL"];

        var urlWithParams = $"{frontendUrl}/?code={chatCode}";

        return new MessagingExtensionActionResponse
        {
            Task = new TaskModuleContinueResponse
            {
                Value = new TaskModuleTaskInfo
                {
                    Url = urlWithParams,
                    Height = 540,
                    Width = 800,
                    Title = "Intel R",
                },
            },
        };
    }

    private static async Task<string> GetSignInLinkAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        var userTokenClient = turnContext.TurnState.Get<UserTokenClient>();
        var resource = await userTokenClient.GetSignInResourceAsync("BotTeamsAuthADv2", turnContext.Activity as Activity, null, cancellationToken).ConfigureAwait(false);
        return resource.SignInLink;
    }

    private static async Task<TokenResponse> GetTokenResponse(ITurnContext<IInvokeActivity> turnContext, string state, CancellationToken cancellationToken)
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