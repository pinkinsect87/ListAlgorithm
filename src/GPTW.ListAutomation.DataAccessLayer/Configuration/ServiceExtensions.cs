using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GPTW.ListAutomation.DataAccessLayer.Configuration
{
    public static class ServiceExtensions
    {
        public static void AddDbStore(this IServiceCollection services, string dbConnectionString)
        {
            //add db context
            services.AddDbContext<ListAutomationDbConetxt>(options =>
                options.UseSqlServer(dbConnectionString));

            //add EF services
            services.AddEntityFrameworkSqlServer();

            // data layer
            services.AddScoped<IDbContext, ListAutomationDbConetxt>();

            // repositories
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        }
    }
}
