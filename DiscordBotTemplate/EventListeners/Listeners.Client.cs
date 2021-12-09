using DiscordTemplateBot.EventListeners.Attributes;
using DiscordTemplateBot.EventListeners.Common;
using DiscordTemplateBot.Services;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DiscordTemplateBot.EventListeners;

internal static partial class Listeners
{
    [AsyncEventListener(DiscordEventType.ClientErrored)]
    public static Task ClientErrorEventHandlerAsync(Bot _, ClientErrorEventArgs e)
    {
        Exception ex = e.Exception;
        while (ex is AggregateException)
            ex = ex.InnerException ?? ex;

        Log.Error(ex, "Client errored: {EventName}", e.EventName);
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.GuildAvailable)]
    public static Task GuildAvailableEventHandlerAsync(Bot _, GuildCreateEventArgs e)
    {
        Log.Information("Available: {AvailableGuild}", e.Guild);
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.GuildUnavailable)]
    public static Task GuildUnvailableEventHandlerAsync(Bot _, GuildDeleteEventArgs e)
    {
        Log.Warning("Unvailable: {UnvailableGuild}", e.Guild);
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.GuildDownloadCompleted)]
    public static Task GuildDownloadCompletedEventHandlerAsync(Bot _, GuildDownloadCompletedEventArgs e)
    {
        Log.Information("All guilds are now downloaded ({Count} total)", e.Guilds.Count);
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.GuildCreated)]
    public static Task GuildCreateEventHandlerAsync(Bot _, GuildCreateEventArgs e)
    {
        Log.Information("Joined {NewGuild}", e.Guild);
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.SocketOpened)]
    public static Task SocketOpenedEventHandlerAsync(Bot bot, SocketEventArgs _)
    {
        Log.Debug("Socket opened");
        bot.Services.GetRequiredService<BotActivityService>().UptimeInformation.SocketStartTime = DateTimeOffset.Now;
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.SocketClosed)]
    public static Task SocketClosedEventHandlerAsync(Bot _, SocketCloseEventArgs e)
    {
        Log.Debug("Socket closed with code {Code}: {Message}", e.CloseCode, e.CloseMessage);
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.SocketErrored)]
    public static Task SocketErroredEventHandlerAsync(Bot _, SocketErrorEventArgs e)
    {
        Log.Debug(e.Exception, "Socket errored");
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.UnknownEvent)]
    public static Task UnknownEventHandlerAsync(Bot _, UnknownEventArgs e)
    {
        Log.Error("Unknown event ({UnknownEvent}) occured: {@UnknownEventJson}", e.EventName, e.Json);
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.UserUpdated)]
    public static Task UserUpdatedEventHandlerAsync(Bot _, UserUpdateEventArgs e)
    {
        Log.Information("Bot updated");
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.UserSettingsUpdated)]
    public static Task UserSettingsUpdatedEventHandlerAsync(Bot _, UserSettingsUpdateEventArgs e)
    {
        Log.Information("User settings updated");
        return Task.CompletedTask;
    }
}
