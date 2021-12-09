using DiscordTemplateBot.Database;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

namespace DiscordTemplateBot.Services.Common;

public sealed class BotConfig
{
    public const string DefaultLocale = "en-GB";
    public const string DefaultPrefix = ".";


    [JsonProperty("db-config")]
    public BotDbConfig DatabaseConfig { get; set; } = new BotDbConfig();

    [JsonProperty("token")]
    public string? Token { get; set; }

    [JsonProperty("prefix")]
    public string Prefix { get; set; } = DefaultPrefix;

    [JsonProperty("log-level")]
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    [JsonProperty("log-path")]
    public string LogPath { get; set; } = "bot.log";

    [JsonProperty("log-file-rolling")]
    public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;

    [JsonProperty("log-to-file")]
    public bool LogToFile { get; set; } = false;

    [JsonProperty("log-buffer")]
    public bool UseBufferedFileLogger { get; set; } = false;

    [JsonProperty("log-max-files")]
    public int? MaxLogFiles { get; set; }

    [JsonProperty("log-template")]
    public string? CustomLogTemplate { get; set; }
}
