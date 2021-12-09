using DiscordTemplateBot.Common;
using DSharpPlus.Entities;

namespace DiscordTemplateBot.Extensions;

internal static class DiscordChannelExtensions
{
    public static Task<DiscordMessage> EmbedAsync(this DiscordChannel channel, string message, DiscordEmoji? icon = null, DiscordColor? color = null)
    {
        return channel.SendMessageAsync(embed: new DiscordEmbedBuilder {
            Description = $"{icon ?? ""} {message}",
            Color = color ?? DiscordColor.Green
        });
    }

    public static Task<DiscordMessage> InformFailureAsync(this DiscordChannel channel, string message)
    {
        return channel.SendMessageAsync(embed: new DiscordEmbedBuilder {
            Description = $"{Emojis.X} {message}",
            Color = DiscordColor.IndianRed
        });
    }

    public static async Task<DiscordMessage?> GetLastMessageAsync(this DiscordChannel channel)
    {
        if (channel.LastMessageId is null)
            return null;
        IReadOnlyList<DiscordMessage> m = await channel.GetMessagesBeforeAsync(channel.LastMessageId.Value, 1);
        return m.FirstOrDefault();
    }

    public static bool IsNsfwOrNsfwName(this DiscordChannel channel)
        => channel.IsNSFW || channel.Name.StartsWith("nsfw", StringComparison.InvariantCultureIgnoreCase);
}
