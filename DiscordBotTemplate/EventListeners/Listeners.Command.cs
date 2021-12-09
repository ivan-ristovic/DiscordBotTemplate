using System.Reflection;
using DiscordTemplateBot.Common;
using DiscordTemplateBot.EventListeners.Attributes;
using DiscordTemplateBot.EventListeners.Common;
using DiscordTemplateBot.Exceptions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

namespace DiscordTemplateBot.EventListeners;

internal static partial class Listeners
{
    [AsyncEventListener(DiscordEventType.CommandExecuted)]
    public static Task CommandExecutionEventHandler(Bot bot, CommandExecutionEventArgs e)
    {
        if (e.Command is null || e.Command.QualifiedName.StartsWith("help"))
            return Task.CompletedTask;

        Log.Information(
            "Executed: {ExecutedCommand} {User} {Guild} {Channel}",
            e.Command.QualifiedName, e.Context.User, e.Context.Guild?.ToString() ?? "DM", e.Context.Channel
        );
        return Task.CompletedTask;
    }

    [AsyncEventListener(DiscordEventType.CommandErrored)]
    public static Task CommandErrorEventHandlerAsync(Bot bot, CommandErrorEventArgs e)
    {
        if (e.Exception is null)
            return Task.CompletedTask;

        Exception ex = e.Exception;
        while (ex is AggregateException or TargetInvocationException && ex.InnerException is { })
            ex = ex.InnerException;

        Log.Debug(
            "Command errored ({ExceptionName}): {ErroredCommand} {User} {Guild} {Channel}",
            e.Exception?.GetType().Name ?? "Unknown", e.Command?.QualifiedName ?? "Unknown",
            e.Context.User, e.Context.Guild?.ToString() ?? "DM", e.Context.Channel
        );

        var emb = new DiscordEmbedBuilder() {
            Title = "Command errored!",
            Description = e.Command?.QualifiedName ?? "",
            Color = DiscordColor.Red,
        };

        switch (ex) {
            case ChecksFailedException _:
            case TaskCanceledException:
                return Task.CompletedTask;
            case CommandNotFoundException:
                return e.Context.Message.CreateReactionAsync(Emojis.Question);
            case UnauthorizedException _:
                emb.WithDescription("403");
                break;
            case NpgsqlException _:
            case DbUpdateException _:
                Log.Error(ex, "Database error");
                return Task.CompletedTask;
            case CommandCancelledException:
                break;
            default:
                emb.WithDescription(ex.Message);
                Log.Error(ex, "Unhandled error");
                break;
        }

        return e.Context.RespondAsync(embed: emb.Build());
    }
}
