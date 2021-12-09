using Newtonsoft.Json;

namespace DiscordTemplateBot.Database;

public sealed class BotDbConfig
{
    [JsonProperty("database")]
    public string DatabaseName { get; set; } = "db_name";

    [JsonProperty("provider")]
    public BotDbProvider Provider { get; set; } = BotDbProvider.Sqlite;

    [JsonProperty("hostname")]
    public string Hostname { get; set; } = "localhost";

    [JsonProperty("password")]
    public string Password { get; set; } = "password";

    [JsonProperty("port")]
    public int Port { get; set; } = 5432;

    [JsonProperty("username")]
    public string Username { get; set; } = "username";
}
