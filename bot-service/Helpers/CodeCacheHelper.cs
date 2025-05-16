using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph.Models;

namespace TestTeamsApp.Helpers;

public class ChatContext
{
    public static readonly List<ChatMessage> MockMessages = [
        new ChatMessage
        {
            Id = "1745479882342",
            Body = new ItemBody
            {
                Content = "那我把代码同步一下？",
                ContentType = BodyType.Html,
            },
            From = new ChatMessageFromIdentitySet
            {
                User = new Identity
                {
                    DisplayName = "Zeno Wang [SSW]"
                },
            },
            MessageType = ChatMessageType.Message,
            CreatedDateTime = DateTimeOffset.Parse("2025-04-24T07:06:11.748Z")
        },
        new ChatMessage
        {
            Id = "1745478378979",
            Body = new ItemBody
            {
                Content = "ok推一下",
                ContentType = BodyType.Html,
            },
            From = new ChatMessageFromIdentitySet
            {
                User = new Identity
                {
                    DisplayName = "Jim Zheng [SSW]"
                },
            },
            MessageType = ChatMessageType.Message,
            CreatedDateTime = DateTimeOffset.Parse("2025-04-24T07:06:18.979Z")
        },
        new ChatMessage
        {
            Id = "1745478385556",
            Body = new ItemBody
            {
                Content = "已经推到main分支了",
                ContentType = BodyType.Html,
            },
            From = new ChatMessageFromIdentitySet
            {
                User = new Identity
                {
                    DisplayName = "Zeno Wang [SSW]"
                },
            },
            MessageType = ChatMessageType.Message,
            CreatedDateTime = DateTimeOffset.Parse("2025-04-24T07:06:25.556Z")
        },
        new ChatMessage
        {
            Id = "1745478390231",
            Body = new ItemBody
            {
                Content = "确认一下代码变动",
                ContentType = BodyType.Html,
            },
            From = new ChatMessageFromIdentitySet
            {
                User = new Identity
                {
                    DisplayName = "Zeno Wang [SSW]"
                },
            },
            MessageType = ChatMessageType.Message,
            CreatedDateTime = DateTimeOffset.Parse("2025-04-24T07:06:30.231Z")
        },
    ];

    public List<ChatMessage> Messages { get; set; }
    public string TargetUser { get; set; }
    public string TargetMessage { get; set; }
    public string CurrentUser { get; set; }

    public ChatContext(IEnumerable<ChatMessage> messages, string targetUser = "", string targetMessage = "", string currentUser = "")
    {
        Messages = [.. messages];
        TargetUser = targetUser;
        TargetMessage = targetMessage;
        CurrentUser = currentUser;
    }
}

public class CodeCacheHelper
{
    private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public static string GenerateCode(string conversationId)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(conversationId));
            return Convert.ToBase64String(hashBytes).Replace("/", "_").Replace("+", "-").Substring(0, 8);
        }
    }

    public static void StoreMessages(string code, List<ChatMessage> messages, int expirationMinutes = 30)
    {
        var chatContext = new ChatContext(messages);
        StoreContext(code, chatContext, expirationMinutes);
    }

    public static void StoreContext(string code, ChatContext context, int expirationMinutes = 30)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(expirationMinutes));

        _cache.Set(code, context, cacheEntryOptions);
    }

    public static List<ChatMessage> GetMessages(string code)
    {
        if (_cache.TryGetValue(code, out ChatContext context))
        {
            return context.Messages;
        }

        if (_cache.TryGetValue(code, out List<ChatMessage> messages))
        {
            return messages;
        }

        return null;
    }

    public static ChatContext GetContext(string code)
    {
        if (_cache.TryGetValue(code, out ChatContext context))
        {
            return context;
        }

        if (_cache.TryGetValue(code, out List<ChatMessage> messages))
        {
            return new ChatContext(messages);
        }

        return null;
    }
}
