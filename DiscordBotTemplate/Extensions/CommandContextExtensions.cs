using DiscordTemplateBot.Common;
using DiscordTemplateBot.Exceptions;
using DiscordTemplateBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordTemplateBot.Extensions;

internal static class CommandContextExtensions
{
    public static Task InfoAsync(this CommandContext ctx, DiscordColor color)
        => InternalInformAsync(ctx, null, null, false, color);

    public static Task InfoAsync(this CommandContext ctx, string? msg = null)
        => InternalInformAsync(ctx, null, msg, false, null);

    public static Task InfoAsync(this CommandContext ctx, DiscordColor color, string msg)
        => InternalInformAsync(ctx, null, msg, false, color);

    public static Task InfoAsync(this CommandContext ctx, DiscordEmoji emoji, string msg)
        => InternalInformAsync(ctx, emoji, msg, false, null);

    public static Task InfoAsync(this CommandContext ctx, DiscordColor color, DiscordEmoji emoji, string msg)
        => InternalInformAsync(ctx, emoji, msg, false, color);

    public static Task ImpInfoAsync(this CommandContext ctx, string? msg = null)
        => InternalInformAsync(ctx, null, msg, true, null);

    public static Task ImpInfoAsync(this CommandContext ctx, DiscordColor color, string msg)
        => InternalInformAsync(ctx, null, msg, true, color);

    public static Task ImpInfoAsync(this CommandContext ctx, DiscordEmoji emoji, string msg)
        => InternalInformAsync(ctx, emoji, msg, true, null);

    public static Task ImpInfoAsync(this CommandContext ctx, DiscordColor color, DiscordEmoji emoji, string msg)
        => InternalInformAsync(ctx, emoji, msg, true, color);

    public static Task FailAsync(this CommandContext ctx, string msg)
    {
        return ctx.RespondAsync(embed: new DiscordEmbedBuilder {
            Description = $"{Emojis.X} {msg}",
            Color = DiscordColor.IndianRed
        });
    }

    public static Task PaginateAsync<T>(this CommandContext ctx, string title, IEnumerable<T> collection,
                                        Func<T, string> selector, DiscordColor? color = null, int pageSize = 10)
    {
        T[] arr = collection.ToArray();

        var pages = new List<Page>();
        int pageCount = (arr.Length - 1) / pageSize + 1;
        int from = 0;
        for (int i = 1; i <= pageCount; i++) {
            int to = from + pageSize > arr.Length ? arr.Length : from + pageSize;
            pages.Add(new Page(embed: new DiscordEmbedBuilder {
                Title = title,
                Description = arr[from..to].Select(selector).JoinWith(),
                Color = color ?? DiscordColor.Black,
                Footer = new DiscordEmbedBuilder.EmbedFooter {
                    Text = $"Showing {from + 1}-{to} out of {arr.Length} ; Page {i}/{pageCount}",
                }
            }));
            from += pageSize;
        }

        return pages.Count > 1
            ? ctx.Client.GetInteractivity().SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages)
            : ctx.Channel.SendMessageAsync(content: pages.First().Content, embed: pages.First().Embed);
    }

    public static Task PaginateAsync<T>(this CommandContext ctx, IEnumerable<T> collection,
                                        Func<DiscordEmbedBuilder, T, DiscordEmbedBuilder> formatter, DiscordColor? color = null)
    {
        int count = collection.Count();

        IEnumerable<Page> pages = collection
            .Select((e, i) => {
                var emb = new DiscordEmbedBuilder();
                emb.WithFooter($"Showing #{i + 1} out of {count}", null);
                emb.WithColor(color ?? DiscordColor.Black);
                emb = formatter(emb, e);
                return new Page { Embed = emb.Build() };
            });

        return count > 1
            ? ctx.Client.GetInteractivity().SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages)
            : ctx.Channel.SendMessageAsync(content: pages.Single().Content, embed: pages.Single().Embed);
    }

    public static async Task<bool> WaitForBoolReplyAsync(this CommandContext ctx, string q, DiscordChannel? channel = null, bool reply = true)
    {
        channel ??= ctx.Channel;

        await ctx.RespondAsync(embed: new DiscordEmbedBuilder {
            Description = $"{Emojis.Question} {q} (y/n)",
            Color = DiscordColor.Yellow
        });

        if (await ctx.Client.GetInteractivity().WaitForBoolReplyAsync(ctx))
            return true;

        if (reply)
            await channel.InformFailureAsync("Aborting");

        return false;
    }

    public static async Task<DiscordMessage?> WaitForDmReplyAsync(this CommandContext ctx, DiscordDmChannel dm, DiscordUser user, TimeSpan? waitInterval = null)
    {
        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        InteractivityService interactivityService = ctx.Services.GetRequiredService<InteractivityService>();

        interactivityService.AddPendingResponse(ctx.Channel.Id, user.Id);
        InteractivityResult<DiscordMessage> mctx = await interactivity.WaitForMessageAsync(m => m.Channel == dm && m.Author == user, waitInterval);
        if (interactivityService is { } && !interactivityService.RemovePendingResponse(ctx.Channel.Id, user.Id))
            throw new ConcurrentOperationException("Failed to remove user from pending list");

        return mctx.TimedOut ? null : mctx.Result;
    }

    public static Task ExecuteOtherCommandAsync(this CommandContext ctx, string command, params string?[] args)
    {
        string callStr = $"{command} {args.JoinWith(" ")}";
        Command cmd = ctx.CommandsNext.FindCommand(callStr, out string actualArgs);
        CommandContext fctx = ctx.CommandsNext.CreateFakeContext(ctx.User, ctx.Channel, callStr, ctx.Prefix, cmd, actualArgs);
        return ctx.CommandsNext.ExecuteCommandAsync(fctx);
    }

    private static async Task InternalInformAsync(this CommandContext ctx, DiscordEmoji? emoji = null, string? msg = null,
                                                  bool important = true, DiscordColor? color = null)
    {
        emoji ??= Emojis.CheckMarkSuccess;
        if (!important) {
            try {
                await ctx.Message.CreateReactionAsync(Emojis.CheckMarkSuccess);
            } catch (NotFoundException) {
                await ImpInfoAsync(ctx, emoji, "Done");
            }
        } else {
            string response = msg ?? "Done";
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder {
                Description = $"{emoji} {response}",
                Color = color ?? DiscordColor.Green,
            });
        }
    }
}
