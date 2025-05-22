namespace IntelR.Host;

public class McpOptions
{
    public GitHubMcpServerOptions GitHubServer { get; set; } = new();
}

public class GitHubMcpServerOptions
{
    public const string Name = "GitHubMcp";

    public string Pat { get; set; } = string.Empty;

    public string Identity { get; set; } = string.Empty;

    public List<string> AllowedRepositories { get; set; } = [];
}