using Atheon.Extensions;
using Atheon.Options;
using Atheon.Services;
using Atheon.Services.BungieApi;
using Atheon.Services.Db.Sqlite;
using Atheon.Services.EventBus;
using Atheon.Services.Hosted;
using Atheon.Services.Interfaces;
using Atheon.Services.Scanners.DestinyClanMemberScanner;
using Atheon.Services.Scanners.DestinyClanScanner;
using Atheon.Services.Scanners.ProfileUpdaters;
using DotNetBungieAPI.HashReferences;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using Serilog;
using Serilog.Exceptions;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log.txt", shared: true)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    ConfigureServices(builder);

    var application = builder.Build();

    ConfigureApplication(application);

    await application.RunAsync();
}
catch (Exception exception)
{
    Console.WriteLine(exception.Message);
    Log.Logger.Error(exception, "Failed to start app");
    await Task.Delay(2000);
    Console.WriteLine("Press any key to exit app...");
    Console.ReadKey();
}

void ConfigureServices(WebApplicationBuilder applicationBuilder)
{
    applicationBuilder.Services.AddControllersWithViews();
    applicationBuilder.Services.AddSwaggerGen();

    applicationBuilder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console();
    });

    applicationBuilder.Services.AddSingleton(typeof(IEventBus<>), typeof(EventBus<>));
    applicationBuilder.Services.AddSingleton<ICommonEvents, CommonEvents>();
    applicationBuilder.Services.AddMemoryCacheWithCleanup();

    applicationBuilder.Services.AddDiscordServices();

    applicationBuilder.Services.Configure<DatabaseOptions>((settings) =>
    {
        applicationBuilder.Configuration.GetSection("Database").Bind(settings);
    });

    switch (applicationBuilder.Configuration.GetSection("Database:CurrentMode").Value)
    {
        case DatabaseOptions.SqliteKey:
            {
                applicationBuilder.Services.AddSingleton<IDbConnectionFactory, SqliteDbConnectionFactory>();
                applicationBuilder.Services.AddSingleton<IDbBootstrap, SqliteDbBootstrap>();
                applicationBuilder.Services.AddSingleton<ISettingsStorage, SqliteSettingsStorage>();
                applicationBuilder.Services.AddSingleton<IDbAccess, SqliteDbAccess>();
                applicationBuilder.Services.AddSingleton<IDbDataValidator, SqliteDbDataValidator>();
                applicationBuilder.Services.AddSingleton<IClansToScanProvider, SqliteClansToScanProvider>();

                applicationBuilder.Services.AddSingleton<IDestinyDb, SqliteDestinyDb>();
                break;
            }
    }

    applicationBuilder.Services.AddSingleton<IBungieClientProvider, BungieClientProvider>();
    applicationBuilder.Services.AddSingleton<BungieNetApiCallHandler>();
    applicationBuilder.Services.AddSingleton<BroadcastSaver>();
    applicationBuilder.Services.AddSingleton<DestinyDefinitionDataService>();
    applicationBuilder.Services.AddSingleton<CuratedDefinitionInitialiser>();

    applicationBuilder.Services.AddSingleton<DestinyInitialClanScanner>();
    applicationBuilder.Services.AddSingleton<DestinyClanScanner>();
    applicationBuilder.Services.AddSingleton<DestinyClanMemberBroadcastedScanner>();
    applicationBuilder.Services.AddSingleton<DestinyClanMemberSilentScanner>();

    applicationBuilder.Services.AddSingleton<IProfileUpdater, CollectibleUpdater>();
    applicationBuilder.Services.AddSingleton<IProfileUpdater, ProgressionUpdater>();
    applicationBuilder.Services.AddSingleton<IProfileUpdater, RecordUpdater>();

    applicationBuilder.Services.AddHostedService<ApplicationStartup>();

    applicationBuilder.Services.AddHostedServiceWithInterface<IUserQueue, UserQueueBackgroundProcessor>();
    applicationBuilder.Services.AddHostedServiceWithInterface<IClanQueue, ClanQueueBackgroundProcessor>();
    applicationBuilder.Services.AddHostedService<BroadcastBackgroundProcessor>();
}

void ConfigureApplication(WebApplication app)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller}/{action=Index}/{id?}");

    app.MapFallbackToFile("index.html");
}