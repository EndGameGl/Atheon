﻿using Microsoft.Extensions.DependencyInjection;

namespace Atheon.DataAccess.Sqlite;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteDbAccess(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IDbConnectionFactory, SqliteDbConnectionFactory>();
        serviceCollection.AddSingleton<IDbBootstrap, SqliteDbBootstrap>();
        serviceCollection.AddSingleton<ISettingsStorage, SqliteSettingsStorage>();
        serviceCollection.AddSingleton<IDbAccess, SqliteDbAccess>();

        serviceCollection.AddSingleton<IDestinyDb, SqliteDestinyDb>();

        return serviceCollection;
    }
}