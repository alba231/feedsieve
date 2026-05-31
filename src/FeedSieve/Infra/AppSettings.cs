namespace FeedSieve.Infra;

public class AppSettings
{
    public required LogSettings Log { get; set; }
}

public class LogSettings
{
    public bool Debug { get; set; }
    public required string TelegramBotToken { get; init; }
    public required string TelegramChatId { get; init; }
    public required string SentryDsn { get; init; }
}
