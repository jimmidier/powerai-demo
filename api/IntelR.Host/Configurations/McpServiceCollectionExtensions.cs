#pragma warning disable IDE0130 // Namespace does not match folder structure
using IntelR.Host;
using Microsoft.SemanticKernel;

namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class McpServiceCollectionExtensions
{
    public static IServiceCollection ConfigureMcp(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<McpOptions>(configuration.GetSection("Mcp"));

        return services;
    }
}