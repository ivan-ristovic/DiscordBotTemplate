using DiscordTemplateBot.Database.Models;
using DiscordTemplateBot.Exceptions;
using DiscordTemplateBot.Extensions;
using DiscordTemplateBot.Modules.Owner.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Serilog;

namespace DiscordTemplateBot.Modules.Owner;

[Group("privilegedusers"), Hidden]
[Aliases("pu", "privu", "privuser", "pusers", "puser", "pusr")]
[RequireOwner]
public sealed class PrivilegedUsersModule : BotServiceModule<PrivilegedUserService>
{
    #region privilegedusers
    [GroupCommand, Priority(1)]
    public Task ExecuteGroupAsync(CommandContext ctx)
        => this.ListAsync(ctx);

    [GroupCommand, Priority(0)]
    public Task ExecuteGroupAsync(CommandContext ctx,
                                 [Description("Users")] params DiscordUser[] users)
        => this.AddAsync(ctx, users);
    #endregion

    #region privilegedusers add
    [Command("add")]
    [Aliases("register", "reg", "new", "a", "+", "+=", "<<", "<", "<-", "<=")]
    public async Task AddAsync(CommandContext ctx,
                              [Description("Users")] params DiscordUser[] users)
    {
        if (users is null || !users.Any())
            throw new InvalidCommandUsageException("You need to provide atleast one user");

        await this.Service.AddAsync(users.Select(u => new PrivilegedUser { UserId = u.Id }));
        await ctx.InfoAsync();
    }
    #endregion

    #region privilegedusers delete
    [Command("delete")]
    [Aliases("unregister", "remove", "rm", "del", "d", "-", "-=", ">", ">>", "->", "=>")]
    public async Task DeleteAsync(CommandContext ctx,
                                 [Description("Users")] params DiscordUser[] users)
    {
        if (users is null || !users.Any())
            throw new InvalidCommandUsageException("You need to provide atleast one user");

        await this.Service.RemoveAsync(users.Select(u => new PrivilegedUser { UserId = u.Id }));
        await ctx.InfoAsync();
    }
    #endregion

    #region privilegedusers list
    [Command("list")]
    [Aliases("print", "show", "view", "ls", "l", "p")]
    public async Task ListAsync(CommandContext ctx)
    {
        IReadOnlyList<PrivilegedUser> privileged = await this.Service.GetAsync();

        var notFound = new List<PrivilegedUser>();
        var valid = new List<DiscordUser>();
        foreach (PrivilegedUser pu in privileged) {
            try {
                DiscordUser user = await ctx.Client.GetUserAsync(pu.UserId);
                valid.Add(user);
            } catch (NotFoundException) {
                Log.Debug("Found 404 privileged user: {UserId}", pu.UserId);
                notFound.Add(pu);
            }
        }

        if (!valid.Any())
            throw new CommandFailedException("No privileged users!");

        await ctx.PaginateAsync(
            "Privileged users:",
            valid,
            user => user.ToString(),
            DiscordColor.Gold,
            10
        );

        Log.Information("Removing {Count} not found privileged users", notFound.Count);
        await this.Service.RemoveAsync(notFound);
    }
    #endregion
}
