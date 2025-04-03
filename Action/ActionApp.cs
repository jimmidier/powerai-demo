using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using AdaptiveCards;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Microsoft.Bot.Connector.Authentication;
using TestTeamsApp.Helpers;

namespace TestTeamsApp.Action;

public class ActionApp : TeamsActivityHandler
{
    private readonly string _adaptiveCardFilePath = Path.Combine(".", "Resources", "helloWorldCard.json");
    // Action.
    protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
    {
        // The user has chosen to create a card by choosing the 'Create Card' context menu command.
        var actionData = ((JObject)action.Data).ToObject<CardResponse>();
        var templateJson = await System.IO.File.ReadAllTextAsync(_adaptiveCardFilePath, cancellationToken);
        var template = new AdaptiveCards.Templating.AdaptiveCardTemplate(templateJson);
        var adaptiveCardJson = template.Expand(new { title = actionData.Title ?? "", subTitle = actionData.SubTitle ?? "", text = actionData.Text ?? "" });
        var adaptiveCard = AdaptiveCard.FromJson(adaptiveCardJson).Card;
        var attachments = new MessagingExtensionAttachment()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = adaptiveCard
        };

        Console.WriteLine($"---chat id: {turnContext.Activity.Conversation.Id}");
        // var activityValue = turnContext.Activity.Value;
        // Console.WriteLine($"--activity value: {JsonSerializer.Serialize(activityValue)}");

        return new MessagingExtensionActionResponse
        {
            ComposeExtension = new MessagingExtensionResult
            {
                Type = "result",
                AttachmentLayout = "list",
                Attachments = new[] { attachments }
            }
        };
    }

    protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
    {
        var state = action.State; // Check the state value
        var tokenResponse = await GetTokenResponse(turnContext, state, cancellationToken);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
        {
            // There is no token, so the user has not signed in yet.

            // Retrieve the OAuth Sign in Link to use in the MessagingExtensionResult Suggested Actions
            var signInLink = await GetSignInLinkAsync(turnContext, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"----sign in link: {signInLink}");

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

        Console.WriteLine($"---token: {tokenResponse.Token}");

        var client = new GraphClient(tokenResponse.Token);
        var chatMessages = await client.GetChatMessagesAsync(turnContext.Activity.Conversation.Id, cancellationToken);
        
        // 使用 ChatMessageHelper 处理消息
        var formattedMessages = ChatMessageHelper.FormatChatMessages(chatMessages);
        
        // 将聊天消息转换为JSON格式并打印到控制台
        var jsonContent = ChatMessageHelper.ConvertToJsonFormat(chatMessages);
        Console.WriteLine("---Chat messages in JSON format:");
        Console.WriteLine(jsonContent);

        return new MessagingExtensionActionResponse
        {
            Task = new TaskModuleContinueResponse
            {
                Value = new TaskModuleTaskInfo
                {
                    Card = GetChatMessagesCard(formattedMessages),
                    Height = 250,
                    Width = 400,
                    Title = "Chat Messages",
                },
            },
        };
    }

    private static Attachment GetChatMessagesCard(IEnumerable<string> messageContents)
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));

        card.Body.Add(new AdaptiveTextBlock()
        {
            Text = $"聊天记录",
            Size = AdaptiveTextSize.ExtraLarge
        });

        // 取最新的五条消息显示
        var recentMessages = messageContents.TakeLast(5);
        
        card.Body.Add(new AdaptiveRichTextBlock()
        {
            Inlines = [.. recentMessages.Select(m => new AdaptiveTextRun(m)).Cast<AdaptiveInline>()]
        });

        return new Attachment()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = card,
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

        if (!string.IsNullOrEmpty(state))
        {
            if (int.TryParse(state, out var parsed))
            {
                magicCode = parsed.ToString();
            }
        }

        var userTokenClient = turnContext.TurnState.Get<UserTokenClient>();
        var tokenResponse = await userTokenClient.GetUserTokenAsync(turnContext.Activity.From.Id, "BotTeamsAuthADv2", turnContext.Activity.ChannelId, magicCode, cancellationToken).ConfigureAwait(false);
        return tokenResponse;
    }

}

internal class CardResponse
{
    public string Title { get; set; }
    public string SubTitle { get; set; }
    public string Text { get; set; }
}