using DiscordTemplateBot.Database.Models;
using DiscordTemplateBot.Exceptions;
using DiscordTemplateBot.Extensions;
using DiscordTemplateBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace DiscordTemplateBot.Modules.Owner;

[Group("status"), Hidden]
[Aliases("statuses", "botstatus", "activity", "activities")]
[RequireOwner]
public sealed class StatusModule : BotServiceModule<BotActivityService>
{
    #region status
    [GroupCommand, Priority(1)]
    public Task ExecuteGroupAsync(CommandContext ctx)
        => this.ListAsync(ctx);

    [GroupCommand, Priority(0)]
    public Task ExecuteGroupAsync(CommandContext ctx,
                                 [Description("Activity type")] ActivityType activity,
                                 [RemainingText, Description("Status")] string status)
        => this.SetAsync(ctx, activity, status);
    #endregion

    #region status add
    [Command("add")]
    [Aliases("register", "reg", "new", "a", "+", "+=", "<<", "<", "<-", "<=")]
    public async Task AddAsync(CommandContext ctx,
                              [Description("Activity type")] ActivityType activity,
                              [RemainingText, Description("Status")] string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new InvalidCommandUsageException("Missing status string!");

        if (status.Length > BotStatus.StatusLimit)
            throw new CommandFailedException($"Status string cannot exceed {BotStatus.StatusLimit} characters!");

        await this.Service.AddAsync(new BotStatus {
            Activity = activity,
            Status = status
        });
        await ctx.InfoAsync();
    }
    #endregion

    #region status delete
    [Command("delete")]
    [Aliases("unregister", "remove", "rm", "del", "d", "-", "-=", ">", ">>", "->", "=>")]
    public async Task DeleteAsync(CommandContext ctx,
                                 [Description("Status id(s)")] params int[] ids)
    {
        await this.Service.RemoveAsync(ids);
        await ctx.InfoAsync();
    }
    #endregion

    #region status list
    [Command("list")]
    [Aliases("print", "show", "view", "ls", "l", "p")]
    public async Task ListAsync(CommandContext ctx)
    {
        IReadOnlyList<BotStatus> statuses = await this.Service.GetAsync();
        await ctx.PaginateAsync(
            "Statuses:",
            statuses,
            s => $"{Formatter.InlineCode($"{s.Id:D2}")}: {s.Activity} - {s.Status}",
            DiscordColor.Yellow,
            10
        );
    }
    #endregion

    #region status set
    [Command("set"), Priority(1)]
    [Aliases("s")]
    public async Task SetAsync(CommandContext ctx,
                              [Description("Activity type")] ActivityType type,
                              [RemainingText, Description("Status")] string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new InvalidCommandUsageException("Missing status string!");

        if (status.Length > BotStatus.StatusLimit)
            throw new CommandFailedException($"Status string cannot exceed {BotStatus.StatusLimit} characters!");

        var activity = new DiscordActivity(status, type);

        this.Service.StatusRotationEnabled = false;
        await ctx.Client.UpdateStatusAsync(activity);
        await ctx.InfoAsync();
    }

    [Command("set"), Priority(0)]
    public async Task SetAsync(CommandContext ctx,
                              [Description("Status id(s)")] int id)
    {
        BotStatus? status = await this.Service.GetAsync(id);
        if (status is null)
            throw new InvalidCommandUsageException("Cannot find status with such ID!");

        var activity = new DiscordActivity(status.Status, status.Activity);

        this.Service.StatusRotationEnabled = false;
        await ctx.Client.UpdateStatusAsync(activity);
        await ctx.InfoAsync();
    }
    #endregion

    #region status setrotation
    [Command("setrotation")]
    [Aliases("sr", "setr", "rotate")]
    public Task SetRotationAsync(CommandContext ctx,
                                [Description("Enable?")] bool enable = true)
    {
        this.Service.StatusRotationEnabled = enable;
        return ctx.ImpInfoAsync($"Status rotation enabled: {this.Service.StatusRotationEnabled}");
    }
    #endregion
}
