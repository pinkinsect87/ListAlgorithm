using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GPTW.ListAutomation.TestUI
{
    internal class StartProgram : BackgroundService
    {
        private readonly IServiceProvider _services;

        public StartProgram(IServiceProvider services)
        {
            _services = services;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            using (var serviceCope = _services.CreateScope())
            {
                var frmMain = serviceCope.ServiceProvider.GetService<FrmMain>();
                Application.Run(frmMain);
            }

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Application.Exit();
            // Force the process to end and exit
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            System.Environment.Exit(0);

            return base.StopAsync(cancellationToken);
        }
    }
}
