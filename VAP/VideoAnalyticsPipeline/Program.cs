using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using VideoAnalyticsPipeline;

await CreateHostBuilder(args)
    .Build()
    .RunAsync();

static IHostBuilder CreateHostBuilder(string[] args) =>

   Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostContext, config) =>
        {
            config.AddJsonFile("appsettings.serilog.json", optional: false, reloadOnChange: true);         
        })
       .ConfigureServices((hostContext, services) =>
       {
           var logger = new LoggerConfiguration()
                            .ReadFrom.Configuration(hostContext.Configuration)                            
                            .CreateLogger();

           services.AddLogging(loggingBuilder =>
           {
               loggingBuilder.ClearProviders();
               loggingBuilder.AddSerilog(logger);
           });

           services.AddPipelineComponents(logger, hostContext.Configuration);
           services.AddHttpClientPolicy(logger, hostContext.Configuration);

       });
