using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

namespace IntelR.Host;

public class KernelFactory(Kernel kernel, ILoggerFactory loggerFactory, IOptions<McpOptions> mcpOptions)
{
    private readonly Kernel _kernel = kernel;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly McpOptions _mcpOptions = mcpOptions.Value;

    public async Task<Kernel> GetKernelAsync()
    {
        if (_kernel.Plugins.Any(p => p.Name == GitHubMcpServerOptions.Name))
        {
            return _kernel;
        }

        var mcpClient = await McpClientFactory.CreateAsync(
            new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = GitHubMcpServerOptions.Name,
                Command = "docker",
                Arguments = [
                    "run",
                    "-i",
                    "--rm",
                    "-e",
                    "GITHUB_PERSONAL_ACCESS_TOKEN",
                    "ghcr.io/github/github-mcp-server"
                    ],
                EnvironmentVariables = new Dictionary<string, string>
                {
                    { "GITHUB_PERSONAL_ACCESS_TOKEN", _mcpOptions.GitHubServer.GitHubPat }
                }
            }),
            loggerFactory: _loggerFactory
        );

        var tools = await mcpClient.ListToolsAsync();
    
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        _kernel.Plugins.AddFromFunctions(GitHubMcpServerOptions.Name, tools.Select(tool => tool.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        return _kernel;
    }
}