using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Connector.Authentication;
using TestTeamsApp.Helpers;

namespace TestTeamsApp.Action;

public class ActionApp(IConfiguration configuration) : TeamsActivityHandler
{
    private readonly IConfiguration _configuration = configuration;

    protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
    {
        var tokenStartTime = DateTime.Now;
        // Console.WriteLine($"开始获取token: {tokenStartTime:yyyy-MM-dd HH:mm:ss.fff}");

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
        // var tokenEndTime = DateTime.Now;
        // Console.WriteLine($"获取token结束: {tokenEndTime:yyyy-MM-dd HH:mm:ss.fff}");
        // Console.WriteLine($"获取token总耗时: {(tokenEndTime - tokenStartTime).TotalMilliseconds} 毫秒");


        // var chatStartTime = DateTime.Now;
        // Console.WriteLine($"开始创建Graph Client并获取聊天记录: {chatStartTime:yyyy-MM-dd HH:mm:ss.fff}");

        int messageCount = 20;

        var client = new GraphClient(tokenResponse.Token);

        var chatMessages = await client.GetChatMessagesAsync(turnContext.Activity.Conversation.Id, messageCount, cancellationToken);

        var targetMessage = "";
        var targetUser = "";
        if (action.MessagePayload != null)
        {
            targetMessage = ChatMessageHelper.StripHtmlTags(action.MessagePayload.Body.Content ?? "");
            targetUser = action.MessagePayload.From.User.DisplayName ?? "";
            // Console.WriteLine($"从消息上下文调用，目标消息发送者: {targetUser}");
            // Console.WriteLine($"从消息上下文调用，目标消息内容: {targetMessage}");
        }

        var currentUser = await client.GetCurrentUserAsync(cancellationToken);
        var userName = currentUser.DisplayName ?? "";

        var chatCode = CodeCacheHelper.GenerateCode(turnContext.Activity.Conversation.Id);

        var chatContext = new ChatContext(
            chatMessages,
            targetUser,
            targetMessage,
            userName
        );
        CodeCacheHelper.StoreContext(chatCode, chatContext);

        // var chatEndTime = DateTime.Now;
        // Console.WriteLine($"获取聊天记录结束: {chatEndTime:yyyy-MM-dd HH:mm:ss.fff}");
        // Console.WriteLine($"获取聊天记录总耗时: {(chatEndTime - chatStartTime).TotalMilliseconds} 毫秒");

        var frontendUrl = _configuration["FRONTEND_URL"];

        Console.WriteLine($"生成的短代码: {chatCode}");

        var urlWithParams = $"{frontendUrl}/?code={chatCode}";
        Console.WriteLine($"Url: {urlWithParams}");

        return new MessagingExtensionActionResponse
        {
            Task = new TaskModuleContinueResponse
            {
                Value = new TaskModuleTaskInfo
                {
                    Url = urlWithParams,
                    Height = 540,
                    Width = 800,
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