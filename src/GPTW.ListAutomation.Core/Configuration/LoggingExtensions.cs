using Destructurama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Json;

namespace GPTW.ListAutomation.Core.Configuration;

public static class LoggingExtensions
{
    public static void AddSerilogLogging(this IServiceCollection services, ILoggingSettings settings)
    {
        var config = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Destructure.JsonNetTypes()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net", LogEventLevel.Warning);

        if (settings.LogToFile)
        {
            config.WriteTo.File(new JsonFormatter(), Path.Combine(AppContext.BaseDirectory, "logs", $"{settings.LogFileName.ToLower()}-.log"),
                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7,
                    restrictedToMinimumLevel: settings.MinimumLogLevel)
                .WriteTo.Async(w => w.Console(new JsonFormatter()));
        }

        Log.Logger = config.CreateLogger();
        var factory = new SerilogLoggerFactory(Log.Logger, true);

        services.AddSingleton<ILoggerFactory>(factory)
            .AddLogging(lb =>
            {
                lb.AddSerilog(Log.Logger, true);
            });
    }
}
