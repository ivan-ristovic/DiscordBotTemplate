using DiscordTemplateBot.Database.Models;
using DiscordTemplateBot.Services.Common;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DiscordTemplateBot.Services;

public sealed class PeriodicTasksService : IDisposable
{
    #region Callbacks
    private static void BotActivityChangeCallback(object? _)
    {
        if (_ is Bot bot) {
            if (bot.Client is null || bot.Client.CurrentUser is null) {
                Log.Error("BotActivityChangeCallback detected null client/user - this should not happen but is not nececarily an error");
                return;
            }

            BotActivityService bas = bot.Services.GetRequiredService<BotActivityService>();
            if (!bas.StatusRotationEnabled)
                return;

            try {
                BotStatus? status = bas.GetRandomStatus();
                if (status is null)
                    Log.Warning("No extra bot statuses present in the database.");

                DiscordActivity activity = status is { }
                    ? new DiscordActivity(status.Status, status.Activity)
                    : new DiscordActivity($"@{bot.Client?.CurrentUser.Username} help", ActivityType.Playing);

                AsyncExecutionService async = bot.Services.GetRequiredService<AsyncExecutionService>();
                async.Execute(bot.Client!.UpdateStatusAsync(activity));
                Log.Debug("Changed bot status to {ActivityType} {ActivityName}", activity.ActivityType, activity.Name);
            } catch (Exception e) {
                Log.Error(e, "An error occured during activity change");
            }
        } else {
            Log.Error("BotActivityChangeCallback failed to cast sender");
        }
    }

    private static void MiscellaneousActionsCallback(object? _)
    {
        if (_ is Bot bot) {
            if (bot.Client is null) {
                Log.Error("MiscellaneousActionsCallback detected null client - this should not happen");
                return;
            }

            try {
                // TODO add your periodic actions here
            } catch (Exception e) {
                Log.Error(e, "An error occured during misc timer callback");
            }
        } else {
            Log.Error("MiscellaneousActionsCallback failed to cast sender");
        }
    }
    #endregion

    #region Timers
    private Timer BotStatusUpdateTimer { get; set; }
    private Timer MiscActionsTimer { get; set; }
    #endregion


    public PeriodicTasksService(Bot bot, BotConfig cfg)
    {
        this.BotStatusUpdateTimer = new Timer(BotActivityChangeCallback, bot, TimeSpan.FromSeconds(25), TimeSpan.FromMinutes(10));
        this.MiscActionsTimer = new Timer(MiscellaneousActionsCallback, bot, TimeSpan.FromSeconds(35), TimeSpan.FromHours(12));
    }


    public void Dispose()
    {
        this.BotStatusUpdateTimer.Dispose();
        this.MiscActionsTimer.Dispose();
    }
}
