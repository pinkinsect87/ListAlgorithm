using GPTW.ListAutomation.Core.Services;
using GPTW.ListAutomation.Core.Services.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GPTW.ListAutomation.Core.Configuration;

public static class ServiceExtensions
{
    public static void AddCoreServices(this IServiceCollection services, ISettings settings)
    {
        services.AddScoped<IDbConnectionFactory, DapperDbConnectionFactory>();

        services.Scan(x => x
            .FromAssemblyOf<IListAutomationService>()
            .AddClasses(classes => classes.AssignableTo(typeof(IListAutomationService)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        // add distributed memory cache
        services.AddDistributedMemoryCache();

        services.TryAddSingleton<ICacheManager, MemoryCacheManager>();
    }
}
