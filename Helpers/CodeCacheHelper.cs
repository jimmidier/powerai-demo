using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace TestTeamsApp.Helpers;

public class ChatContext<T>
{
    public T Messages { get; set; }
    public string TargetUser { get; set; }
    public string TargetMessage { get; set; }
    public string CurrentUser { get; set; }

    public ChatContext(T messages, string targetUser = "", string targetMessage = "", string currentUser = "")
    {
        Messages = messages;
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

    public static void StoreMessages<T>(string code, T messages, int expirationMinutes = 30)
    {
        var chatContext = new ChatContext<T>(messages);
        StoreContext(code, chatContext, expirationMinutes);
    }

    public static void StoreContext<T>(string code, ChatContext<T> context, int expirationMinutes = 30)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(expirationMinutes));

        _cache.Set(code, context, cacheEntryOptions);
    }

    public static T GetMessages<T>(string code) where T : class
    {
        if (_cache.TryGetValue(code, out ChatContext<T> context))
        {
            return context.Messages;
        }

        if (_cache.TryGetValue(code, out T messages))
        {
            return messages;
        }

        return null;
    }

    public static ChatContext<T> GetContext<T>(string code) where T : class
    {
        if (_cache.TryGetValue(code, out ChatContext<T> context))
        {
            return context;
        }

        if (_cache.TryGetValue(code, out T messages))
        {
            return new ChatContext<T>(messages);
        }

        return null;
    }
}
