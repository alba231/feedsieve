using FeedSieve.Core.Exceptions;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace FeedSieve.Infra;

internal static class LoggerInitializer
{
    public static void Initialize(MauiAppBuilder builder)
    {
        var appSettings = InitAppSettings(builder);
        InitializeSentry(builder, appSettings.Log);
        InitializeSerilog(appSettings.Log);
    }

    private static void InitializeSentry(MauiAppBuilder builder, LogSettings logSettings)
    {
        builder.UseSentry(options =>
            {
                options.Dsn = logSettings.SentryDsn;
                options.Debug = logSettings.Debug;
            });
    }

    private static void InitializeSerilog(LogSettings logSettings)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.File(GetLogPath(), rollingInterval: RollingInterval.Day)
            .WriteTo.Sentry()
            .WriteTo.Telegram(botToken: logSettings.TelegramBotToken, chatId: logSettings.TelegramChatId, restrictedToMinimumLevel: LogEventLevel.Warning);

#if DEBUG
        loggerConfiguration.WriteTo.Debug();
#endif
        Log.Logger = loggerConfiguration.CreateLogger();

    }



    private static AppSettings InitAppSettings(MauiAppBuilder builder)
    {
        builder.Configuration.AddJsonStream(typeof(MauiProgram).Assembly
            .GetManifestResourceStream("FeedSieve.Resources.AppSettings.json")!);
        var localStream = typeof(MauiProgram).Assembly.GetManifestResourceStream("FeedSieve.Resources.AppSettings.local.json");
        if (localStream is not null)
            builder.Configuration.AddJsonStream(localStream);
        var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>() ?? throw new FeedSieveException("Failed to load app settings");
        return appSettings;
    }

    private static string GetLogPath()
    {
        string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FeedSieve", "Logs");
        Directory.CreateDirectory(logDirectory);
        return Path.Combine(logDirectory, "log-.txt");
    }
}