using Atheon.Services.Caching;
using Atheon.Services.DiscordHandlers;
using Atheon.Services.Hosted;
using Atheon.Services.Interfaces;

namespace Atheon.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IDiscordClientProvider, DiscordClientProvider>();
        serviceCollection.AddSingleton<IDiscordEventHandler, DiscordEventHandler>();

        return serviceCollection;
    }

    public static IServiceCollection AddHostedServiceWithInterface<THostedServiceInterface, THostedService>(
        this IServiceCollection serviceCollection)
        where THostedService : class, THostedServiceInterface, IHostedService
        where THostedServiceInterface : class
    {
        serviceCollection.AddSingleton<THostedServiceInterface, THostedService>();

        serviceCollection.AddSingleton<IHostedService>(x =>
            (THostedService)x.GetRequiredService<THostedServiceInterface>());

        return serviceCollection;
    }

    public static IServiceCollection AddMemoryCacheWithCleanup(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IMemoryCache, MemoryCacheImplementation>();
        serviceCollection.AddHostedService<MemoryCacheBackgroundCleaner>();
        serviceCollection.AddSingleton<EmbedBuilderService>();
        return serviceCollection;
    }
}
