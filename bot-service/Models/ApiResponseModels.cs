using System.Collections.Generic;

namespace TestTeamsApp.Models;

public class ApiResponse
{
    public List<string> Suggestions { get; set; } = new List<string>();
    public List<TopicInfo> Topics { get; set; } = new List<TopicInfo>();
}
public class TopicInfo
{
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}