using Atheon.Services.Interfaces;
using Atheon.Services.Discord;

namespace Atheon.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscordServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IDiscordClientProvider, DiscordClientProvider>();
            serviceCollection.AddSingleton<IDiscordEventHandler, DiscordEventHandler>();

            return serviceCollection;
        }
    }
}
