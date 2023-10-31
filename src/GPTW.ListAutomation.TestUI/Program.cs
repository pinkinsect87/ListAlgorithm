using GPTW.ListAutomation.Core;
using GPTW.ListAutomation.Core.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.TestUI.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GPTW.ListAutomation.TestUI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static async Task Main()
        {
            await CreateHostBuilder().Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                var settings = Settings.Build();
                ConfigureServices(services, settings);

                services.AddHostedService<StartProgram>();
            });
        }

        /// <summary>
        /// ConfigureServices
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigureServices(IServiceCollection services, ISettings settings)
        {
            services.AddScoped<FrmMain>();

            services.AddSingleton(settings);

            services.AddCoreServices(settings);

            services.AddSerilogLogging(settings);

            services.AddDbStore(settings.DbConnectionString);

            services.AddScoped<IExcelWorksheetDataHandler, ExcelCommentsDataHandler>();
            services.AddScoped<IExcelWorksheetDataHandler, ExcelCultureBriefDataHandler>();
            services.AddScoped<IExcelWorksheetDataHandler, ExcelOrgSurveyDataHandler>();
            services.AddScoped<IExcelWorksheetDataHandler, ExcelDemographicsDataHandler>();

            services.AddScoped<IListAlgorithmHandler, ListAlgorithmHandler>();
            services.AddScoped<GPTWModules>();
        }
    }
}