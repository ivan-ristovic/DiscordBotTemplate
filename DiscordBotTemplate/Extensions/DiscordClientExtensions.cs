﻿using DSharpPlus;
using DSharpPlus.Entities;

namespace DiscordTemplateBot.Extensions;

internal static class DiscordClientExtensions
{
    public static async Task<DiscordDmChannel?> CreateDmChannelAsync(this DiscordClient client, ulong uid)
    {
        foreach ((ulong _, DiscordGuild guild) in client.Guilds) {
            DiscordMember? member = await guild.GetMemberSilentAsync(uid);
            if (member is { })
                return await member.CreateDmChannelAsync();
        }
        return null;
    }

    public static async Task<DiscordDmChannel?> CreateOwnerDmChannel(this DiscordClient client)
    {
        foreach (DiscordUser owner in client.CurrentApplication.Owners) {
            DiscordDmChannel? dm = await client.CreateDmChannelAsync(owner.Id);
            if (dm is { })
                return dm;
        }
        return null;
    }

    public static bool IsOwnedBy(this DiscordClient client, DiscordUser user)
        => client.CurrentApplication?.Owners.Contains(user) ?? false;
}
