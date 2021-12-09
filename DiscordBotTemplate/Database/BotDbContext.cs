using DiscordTemplateBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordTemplateBot.Database;

public class BotDbContext : DbContext
{
    #region db sets
    public virtual DbSet<BotStatus> BotStatuses { get; protected set; }
    public virtual DbSet<PrivilegedUser> PrivilegedUsers { get; protected set; }
    #endregion

    private BotDbProvider Provider { get; }
    private string ConnectionString { get; }


#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    public BotDbContext(BotDbProvider provider, string connectionString)
    {
        this.Provider = provider;
        this.ConnectionString = connectionString;
    }

    public BotDbContext(BotDbProvider provider, string connectionString, DbContextOptions<BotDbContext> options)
        : base(options)
    {
        this.Provider = provider;
        this.ConnectionString = connectionString;
    }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        switch (this.Provider) {
            case BotDbProvider.PostgreSql:
                optionsBuilder.UseNpgsql(this.ConnectionString);
                break;
            case BotDbProvider.Sqlite:
            case BotDbProvider.SqliteInMemory:
                optionsBuilder.UseSqlite(this.ConnectionString);
                break;
            default:
                throw new NotSupportedException("Selected database provider not supported!");
        }
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("xf");
    }
}
