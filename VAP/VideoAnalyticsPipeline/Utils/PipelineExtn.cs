using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Core;

namespace VideoAnalyticsPipeline;
internal static class PipelineExtn
{
    internal static IServiceCollection AddPipelineComponents(this IServiceCollection services, Logger logger, IConfiguration configuration)
    {
        try
        {
            var modelConfig = ParseModelConfigurations(configuration) ?? throw new Exception("Unable to parse model configurations");
            var pipelineComponentsConfig = ParsePipelineConfig(configuration) ?? throw new Exception("Unable to parse pipeline configurations");

            services.AddSingleton(modelConfig);
            services.AddSingleton(pipelineComponentsConfig);
            services.AddSingleton(provider => new SqlConnection(configuration["ConnectionString:SQLServer"]));

            foreach (var component in pipelineComponentsConfig.Components)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IModule), Type.GetType(component)!));
            }

            services.AddSingleton<ChannelFactory>();
            services.AddSingleton<InferenceRules>();
            services.AddSingleton<MerakiAPIProxy>();
            services.AddSingleton<MailManager>();
            services.AddHostedService<PPEDetectorService>();

        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
        return services;
    }

    private static ModelConfig? ParseModelConfigurations(IConfiguration configuration) =>
    new()
    {
        Models = configuration.GetSection("Models").Get<Dictionary<string, ModelInference>>()
    };

    private static PipelineComponentsConfig? ParsePipelineConfig(IConfiguration configuration) =>
    new()
    {
        PipelineComponents = configuration.GetSection("Pipeline").Get<Dictionary<string, string[]>>()
    };
}
