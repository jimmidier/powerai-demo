using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using AdaptiveCards;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Connector.Authentication;
using TestTeamsApp.Helpers;

namespace TestTeamsApp.Action;

public class ActionApp : TeamsActivityHandler
{
    private readonly string _adaptiveCardFilePath = Path.Combine(".", "Resources", "helloWorldCard.json");
    
    protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
    {
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
        int suggestedReplyCount = 3;
        var client = new GraphClient(tokenResponse.Token);

        var chatMessages = await client.GetChatMessagesAsync(turnContext.Activity.Conversation.Id, cancellationToken, messageCount);
        var formattedMessages = ChatMessageHelper.FormatChatMessages(chatMessages);

        string chatCode = CodeCacheHelper.GenerateCode(turnContext.Activity.Conversation.Id);
        CodeCacheHelper.StoreMessages(chatCode, chatMessages);

        var currentUser = await client.GetCurrentUserAsync(cancellationToken);
        
        Console.WriteLine($"生成的短代码: {chatCode}");
        Console.WriteLine($"当前用户: {currentUser.DisplayName}");
        
        var suggestedReplies = await ChatMessageHelper.GetSuggestedRepliesAsync(chatMessages, currentUser.DisplayName, suggestedReplyCount);

        return new MessagingExtensionActionResponse
        {
            Task = new TaskModuleContinueResponse
            {                Value = new TaskModuleTaskInfo
                {
                    // Url = "https://example.com/",
                    Url = $"https://power-ai-front-end.vercel.app/?code={chatCode}&username={Uri.EscapeDataString(currentUser.DisplayName)}",
                    Height = 500,
                    Width = 600,
                    Title = "Power AI",
                },
            },
        };
    }

    private static Attachment GetChatMessagesCardWithSuggestions(IEnumerable<string> messageContents, List<string> suggestedReplies)
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));

        card.Body.Add(new AdaptiveTextBlock()
        {
            Text = "聊天记录",
            Size = AdaptiveTextSize.Large,
            Weight = AdaptiveTextWeight.Bolder
        });

        var recentMessages = messageContents.TakeLast(5);
        foreach (var message in recentMessages)
        {
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = message,
                Wrap = true
            });
        }
        
        card.Body.Add(new AdaptiveContainer()
        {
            Items = new List<AdaptiveElement>()
            {
                new AdaptiveTextBlock()
                {
                    Text = "─────────────────────────────",
                    Color = AdaptiveTextColor.Accent,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Center
                }
            },
            Spacing = AdaptiveSpacing.Medium
        });
        
        card.Body.Add(new AdaptiveTextBlock()
        {
            Text = "AI 建议回复选项:",
            Size = AdaptiveTextSize.Medium,
            Weight = AdaptiveTextWeight.Bolder,
            Color = AdaptiveTextColor.Accent
        });
        
        for (int i = 0; i < suggestedReplies.Count; i++)
        {
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = $"选项 {i + 1}:",
                Weight = AdaptiveTextWeight.Bolder,
                Spacing = AdaptiveSpacing.Medium
            });
            
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = suggestedReplies[i],
                Wrap = true
            });
            
            card.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = $"使用选项 {i + 1}",
                Data = new { copiedText = suggestedReplies[i], optionNumber = i + 1 }
            });
        }

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

        if (!string.IsNullOrEmpty(state) && int.TryParse(state, out var parsed))
        {
            magicCode = parsed.ToString();
        }

        var userTokenClient = turnContext.TurnState.Get<UserTokenClient>();
        return await userTokenClient.GetUserTokenAsync(turnContext.Activity.From.Id, "BotTeamsAuthADv2", turnContext.Activity.ChannelId, magicCode, cancellationToken).ConfigureAwait(false);
    }
}

internal class CardResponse
{
    public string Title { get; set; }
    public string SubTitle { get; set; }
    public string Text { get; set; }
}