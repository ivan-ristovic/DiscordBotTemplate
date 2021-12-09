using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

namespace DiscordTemplateBot.Database;

public class BotDbContextBuilder
{
    public BotDbProvider Provider { get; }

    private string ConnectionString { get; }
    private DbContextOptions<BotDbContext>? Options { get; }


    public BotDbContextBuilder(BotDbProvider provider, string connectionString, DbContextOptions<BotDbContext>? options = null)
    {
        this.Provider = provider;
        this.ConnectionString = connectionString;
        this.Options = options;
    }

    public BotDbContextBuilder(BotDbConfig cfg, DbContextOptions<BotDbContext>? options = null)
    {
        cfg ??= new BotDbConfig();
        this.Provider = cfg.Provider;
        this.Options = options;
        this.ConnectionString = this.Provider switch {
            BotDbProvider.PostgreSql => new NpgsqlConnectionStringBuilder {
                Host = cfg.Hostname,
                Port = cfg.Port,
                Database = cfg.DatabaseName,
                Username = cfg.Username,
                Password = cfg.Password,
                Pooling = true,
                MaxAutoPrepare = 50,
                AutoPrepareMinUsages = 3,
                SslMode = SslMode.Prefer,
                TrustServerCertificate = true
            }.ConnectionString,
            BotDbProvider.Sqlite => $"Data Source={cfg.DatabaseName}.db;",
            BotDbProvider.SqliteInMemory => @"DataSource=:memory:;foreign keys=true;",
            _ => throw new NotSupportedException("Unsupported database provider!"),
        };
    }


    public BotDbContext CreateContext()
    {
        try {
            return this.Options is null
                ? new BotDbContext(this.Provider, this.ConnectionString)
                : new BotDbContext(this.Provider, this.ConnectionString, this.Options);
        } catch (Exception e) {
            Log.Fatal(e, "An exception occured during database initialization:");
            throw;
        }
    }

    public BotDbContext CreateContext(DbContextOptions<BotDbContext> options)
    {
        try {
            return new BotDbContext(this.Provider, this.ConnectionString, options);
        } catch (Exception e) {
            Log.Fatal(e, "An exception occured during database initialization:");
            throw;
        }
    }
}
