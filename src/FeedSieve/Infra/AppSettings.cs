namespace FeedSieve.Infra;

public class AppSettings
{
    public required LogSettings Log { get; set; }
}

public class LogSettings
{
    public bool Debug { get; set; }
    public string TelegramBotToken { get; init; } = string.Empty;
    public string TelegramChatId { get; init; } = string.Empty;
    public string SentryDsn { get; init; } = string.Empty;
}
