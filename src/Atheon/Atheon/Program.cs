using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);

    ConfigureServices(builder);

    var application = builder.Build();

    await application.RunAsync();
}
catch (Exception exception)
{
    Log.Logger.Error(exception, "Failed to start app");
}

void ConfigureServices(WebApplicationBuilder applicationBuilder)
{
    applicationBuilder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services.GetService<IServiceProvider>())
            .Enrich.FromLogContext()
            .WriteTo.Console();
    });
}