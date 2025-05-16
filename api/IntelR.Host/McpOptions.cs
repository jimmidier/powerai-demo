namespace IntelR.Host;

public class McpOptions
{
    public GitHubMcpServerOptions GitHubServer { get; set; } = new();
}

public class GitHubMcpServerOptions
{
    public const string Name = "GitHubMcp";
    
    public string GitHubPat { get; set; } = string.Empty;
}