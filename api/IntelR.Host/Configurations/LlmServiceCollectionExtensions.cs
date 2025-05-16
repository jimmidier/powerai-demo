#pragma warning disable IDE0130 // Namespace does not match folder structure
using IntelR.Host;
using Microsoft.SemanticKernel;

namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class LlmServiceCollectionExtensions
{
    public static IServiceCollection ConfigureOpenAi(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiConfigurationSection = configuration.GetSection("OpenAI");
        var openAiOptions = openAiConfigurationSection.Get<OpenAIOptions>();

        services.Configure<OpenAIOptions>(openAiConfigurationSection);

        services.AddAzureOpenAIChatCompletion(
            deploymentName: openAiOptions!.DeploymentName,
            apiKey: openAiOptions.ApiKey,
            endpoint: openAiOptions.Endpoint
        );

        return services;
    }
}