using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
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
    Log.Logger.Error(exception, "Failed to start app");
}

void ConfigureServices(WebApplicationBuilder applicationBuilder)
{
    applicationBuilder.Services.AddControllersWithViews();

    applicationBuilder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services.GetService<IServiceProvider>())
            .Enrich.FromLogContext()
            .WriteTo.Console();
    });


}

void ConfigureApplication(WebApplication webApplication)
{
    if (!webApplication.Environment.IsDevelopment())
    {
        webApplication.UseHsts();
    }

    webApplication.UseHttpsRedirection();
    webApplication.UseStaticFiles();
    webApplication.UseRouting();


    webApplication.MapControllerRoute(
        name: "default",
        pattern: "{controller}/{action=Index}/{id?}");

    webApplication.MapFallbackToFile("index.html");
}