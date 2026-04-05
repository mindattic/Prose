using Serilog;
using Serilog.Events;
using StreetSamurai.Blazor.Components;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog — daily rolling log files in engine_data/logs/
var settings = new SettingsService();
var pathProvider = new FileSystemPathProvider(settings);
var logDir = Path.Combine(pathProvider.DataRoot, "logs");
Directory.CreateDirectory(logDir);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: $"{{Timestamp:{settings.TimestampFormat}}} [{{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}")
    .WriteTo.File(
        Path.Combine(logDir, "log-.txt"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: $"{{Timestamp:{settings.TimestampFormat}}} [{{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}",
        retainedFileCountLimit: 90,
        shared: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddStreetSamuraiServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
// Repos load lazily from individual JSON files in engine_data/

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

Log.Information("StreetSamurai Blazor host started");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
