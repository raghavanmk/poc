using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Core;
using System.Net;
using System.Net.Mail;
using VideoAnalyticsPipeline.Components;

namespace VideoAnalyticsPipeline;
internal static class ConfigurePipelineServices
{
    internal static IServiceCollection AddPipelineComponents(this IServiceCollection services, Logger logger, IConfiguration configuration)
    {
        try
        {
            var modelConfig = ParseModelConfigurations(configuration) ?? throw new Exception("Unable to parse model configurations");
            modelConfig.ConfigEmailAlerts();

            var pipelineComponentsConfig = ParsePipelineConfig(configuration) ?? throw new Exception("Unable to parse pipeline configurations");

            services.AddSingleton(modelConfig);
            services.AddSingleton(pipelineComponentsConfig);
            services.AddSingleton(provider => new SqlConnection(configuration["ConnectionString:SQLServer"]));

            foreach (var component in pipelineComponentsConfig.Components)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IModule), Type.GetType(component)!));
            }

            services.AddSingleton<ChannelFactory>();
            services.AddSingleton<InferenceFilter>();
            services.AddSingleton<InferenceRules>();
            services.AddSingleton<MerakiAPIProxy>();
            services.AddSingleton<MailManager>();
            services.AddSingleton<BlobStorage>();
            services.AddHostedService<PPEDetectorService>();

            services.AddSingleton<ISmtpClient>(serviceProvider =>
            {
                string fromPassword = configuration["SMTP:Password"]!;
                string smtpHost = configuration["SMTP:Host"]!;
                int smtpPort = Convert.ToInt16(configuration["SMTP:Port"]);
                string fromAddress = configuration["SMTP:Address"]!;

                var smtpClient = new SmtpClient
                {
                    Host = smtpHost,
                    Port = smtpPort,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress, fromPassword)
                };
                return new SmtpClientWrapper(smtpClient);
            });
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

    private static ModelConfig? ParseModelConfigurations(IConfiguration configuration)
    {
        var emailAlertGroupSection = configuration.GetSection("EmailAlertGroup");
        var emailAlertGroup = new Dictionary<string, string[]>();

        foreach (var child in emailAlertGroupSection.GetChildren())
        {
            emailAlertGroup[child.Key] = child.Get<string[]>() ?? [];
        }

        return new ModelConfig
        {
            ModelInference = configuration.GetSection("ModelInference").Get<Dictionary<string, ModelInference>>(),
            EmailAlertGroup = emailAlertGroup,
            Camera = configuration.GetSection("Camera").Get<Dictionary<string, CameraDetails>>(),
            CameraFilter = configuration.GetSection("CameraFilter").Get<Dictionary<string, CameraFilter>>(),
            LabelMap = configuration.GetSection("LabelMap").Get<Dictionary<string, string>>()?.ToDictionary(kvp => int.Parse(kvp.Key), kvp => kvp.Value),
            CameraRule = configuration.GetSection("CameraRule").Get<Dictionary<string, string[]>>()
        };
    }

    private static PipelineComponentsConfig? ParsePipelineConfig(IConfiguration configuration) =>
    new()
    {
        PipelineComponents = configuration.GetSection("Pipeline").Get<Dictionary<string, string[]>>()
    };
}
