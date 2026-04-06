using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Services;

namespace StreetSamurai.MAUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		// Configure Serilog — daily rolling log files in engine/logs/
		var settings = new SettingsService();
		var pathProvider = new FileSystemPathProvider(settings);
		var logDir = pathProvider.LogDir;

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
			.MinimumLevel.Override("System", LogEventLevel.Warning)
			.WriteTo.File(
				Path.Combine(logDir, "log-.txt"),
				rollingInterval: RollingInterval.Day,
				outputTemplate: $"{{Timestamp:{settings.TimestampFormat}}} [{{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}",
				retainedFileCountLimit: 90,
				shared: true)
			.CreateLogger();

		builder.Logging.AddSerilog(Log.Logger);

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddStreetSamuraiServices();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

		Log.Information("StreetSamurai MAUI host started");

		return builder.Build();
	}
}
