namespace FeedSieve.Infra;

public class AppSettings
{
    public LogSettings Log { get; set; } = null!;
}

public class LogSettings
{
    public bool Debug { get; set; }
    public string TelegramBotToken { get; init; } = string.Empty;
    public string TelegramChatId { get; init; } = string.Empty;
    public string SentryDsn { get; init; } = string.Empty;
}
