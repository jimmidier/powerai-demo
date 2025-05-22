#pragma warning disable IDE0130 // Namespace does not match folder structure
using IntelR.Host.Services;

namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class IntelRServiceCollectionExtensions
{
    public static IServiceCollection ConfigureIntelR(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<ChatHistoryProcessor>();
        services.AddTransient<AIService>();

        return services;
    }
}