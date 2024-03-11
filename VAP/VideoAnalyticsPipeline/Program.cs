using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Runtime.CompilerServices;
using VideoAnalyticsPipeline;

[assembly: InternalsVisibleTo("VideoAnalyticsPipeline.Tests")]
// XUnit wanted this 😊
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2,PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

await CreateHostBuilder(args)
    .Build()
    .RunAsync();

static IHostBuilder CreateHostBuilder(string[] args) =>

   Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostContext, config) =>
        {
            config.AddJsonFile("appsettings.Serilog.json", optional: false, reloadOnChange: true);         
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
