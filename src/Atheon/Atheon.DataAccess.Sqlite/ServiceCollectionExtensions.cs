namespace Atheon.DataAccess.Sqlite;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteDbAccess(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IDbConnectionFactory, SqliteDbConnectionFactory>();
        serviceCollection.AddSingleton<IDbBootstrap, SqliteDbBootstrap>();
        serviceCollection.AddSingleton<ISettingsStorage, SqliteSettingsStorage>();
        serviceCollection.AddSingleton<IDbAccess, SqliteDbAccess>();
        serviceCollection.AddSingleton<IDestinyGroupSearchDb, SqliteDestinyGroupSearchDb>();

        serviceCollection.AddSingleton<IGuildDb, GuildDb>();
        serviceCollection.AddSingleton<IBroadcastDb, SqliteBroadcastDb>();
        serviceCollection.AddSingleton<IServerAdminstrationDb, SqliteServerAdminstrationDb>();
        serviceCollection.AddSingleton<IDestinyDb, SqliteDestinyDb>();

        return serviceCollection;
    }
}
