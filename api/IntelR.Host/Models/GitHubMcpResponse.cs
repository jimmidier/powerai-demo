namespace IntelR.Host.Models;

public class GitHubMcpResponse
{
    public bool IsSuccess { get; set; }

    public string? Result { get; set; }
    
    public string? Explanation{ get; set; }
}