#pragma warning disable IDE0130 // Namespace does not match folder structure
using IntelR.Host;
using Microsoft.SemanticKernel;

namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class KernelServiceCollectionExtensions
{
    public static IServiceCollection ConfigureKernel(this IServiceCollection services)
    {
        services.AddTransient((serviceProvider) => new Kernel(serviceProvider));
        services.AddSingleton<KernelFactory>();

        return services;
    }
}