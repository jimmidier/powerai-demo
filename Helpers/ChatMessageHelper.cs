using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Graph.Models;

namespace TestTeamsApp.Helpers;

public static class ChatMessageHelper
{
    /// <summary>
    /// 格式化聊天消息，移除HTML标签并添加发送者信息
    /// </summary>
    /// <param name="messages">Graph API返回的聊天消息</param>
    /// <returns>格式化后的消息列表</returns>
    public static List<string> FormatChatMessages(IEnumerable<ChatMessage> messages)
    {
        // 只筛选出消息类型为 "message" 的消息，并按时间从早到晚排序
        var filteredMessages = messages
            .Where(m => m.MessageType?.ToString() == "Message")
            .OrderBy(m => m.CreatedDateTime)
            .ToList();
        
        var formattedMessages = new List<string>();
        
        foreach (var message in filteredMessages)
        {
            // 提取发送者名称
            var displayName = message.From?.User?.DisplayName ?? "Unknown User";
            
            // 移除HTML标签
            var plainText = StripHtmlTags(message.Body.Content);
            
            // 格式化消息为 "发送者: 消息内容"
            var formattedMessage = $"{displayName}: {plainText}";
            
            formattedMessages.Add(formattedMessage);
        }
        
        return formattedMessages;
    }
    
    /// <summary>
    /// 移除字符串中的HTML标签
    /// </summary>
    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }
        
        // 使用正则表达式移除HTML标签
        var plainText = Regex.Replace(html, "<[^>]*>", string.Empty);
        
        // 替换HTML实体
        plainText = Regex.Replace(plainText, "&nbsp;", " ");
        plainText = Regex.Replace(plainText, "&amp;", "&");
        plainText = Regex.Replace(plainText, "&lt;", "<");
        plainText = Regex.Replace(plainText, "&gt;", ">");
        
        return plainText.Trim();
    }

    /// <summary>
    /// 将聊天消息转换为指定的JSON格式，只取最新的五条消息
    /// </summary>
    /// <param name="messages">Graph API返回的聊天消息</param>
    /// <returns>格式化后的JSON字符串</returns>
    public static string ConvertToJsonFormat(IEnumerable<ChatMessage> messages)
    {
        // 只筛选出消息类型为 "message" 的消息，并按时间从早到晚排序
        var filteredMessages = messages
            .Where(m => m.MessageType?.ToString() == "Message")
            .OrderBy(m => m.CreatedDateTime)
            .ToList();
            
        // 取最新的五条消息
        var recentMessages = filteredMessages.TakeLast(5).ToList();
            
        // 转换为指定的JSON结构
        var chatHistory = recentMessages.Select(m => new
        {
            sender = m.From?.User?.DisplayName ?? "Unknown User",
            content = StripHtmlTags(m.Body.Content),
            timestamp = m.CreatedDateTime?.ToString("yyyy-MM-ddTHH:mm:ssZ")
        }).ToList();
        
        // 创建最终的包装对象
        var resultObject = new
        {
            chatHistory
        };
        
        // 序列化为JSON，设置不对非ASCII字符进行转义
        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // 美化输出的JSON
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 防止Unicode转义
        };
        
        return JsonSerializer.Serialize(resultObject, options);
    }
}