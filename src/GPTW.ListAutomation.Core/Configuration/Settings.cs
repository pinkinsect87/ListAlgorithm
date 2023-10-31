using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace GPTW.ListAutomation.Core
{
    public interface ILoggingSettings
    {
        LogEventLevel MinimumLogLevel { get; }
        string LogFileName { get; }
        bool LogToFile { get; }
    }

    public interface ISettings : ILoggingSettings
    {
        string DbConnectionString { get; }
        int BulkLoadMaxPerBatch { get; }
        int SqlBulkCopyBatchSize { get; }
    }

    public sealed class Settings : ISettings
    {
        public Settings()
        {
            //default value
            BulkLoadMaxPerBatch = 100000;
            SqlBulkCopyBatchSize = 10000;
            LogToFile = true;
            LogFileName = "ListAutomation";
        }

        public static Settings Build()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var baseConfiguration = builder.Build();
            var settings = baseConfiguration.Get<Settings>(binderOptions =>
            {
                binderOptions.BindNonPublicProperties = true;
            });

            return settings;
        }

        public string DbConnectionString { get; private set; }
        public int BulkLoadMaxPerBatch { get; private set; }
        public int SqlBulkCopyBatchSize { get; private set; }
        public LogEventLevel MinimumLogLevel { get; private set; }
        public string LogFileName { get; private set; }
        public bool LogToFile { get; private set; }
    }
}
