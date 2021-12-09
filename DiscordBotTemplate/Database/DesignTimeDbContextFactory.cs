using DiscordTemplateBot.Services;
using DiscordTemplateBot.Services.Common;
using Microsoft.EntityFrameworkCore.Design;

namespace DiscordTemplateBot.Database;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    private readonly BotConfigService cfg;
    private readonly AsyncExecutionService async;


    public DesignTimeDbContextFactory()
    {
        this.cfg = new BotConfigService();
        this.async = new AsyncExecutionService();
    }


    public BotDbContext CreateDbContext(params string[] _)
    {
        BotConfig cfg = this.async.Execute(this.cfg.LoadConfigAsync("Resources/config.json"));
        return new BotDbContextBuilder(cfg.DatabaseConfig).CreateContext();
    }
}
