using System.Collections.Concurrent;
using DiscordTemplateBot.Common.Collections;

namespace DiscordTemplateBot.Services;

public sealed class InteractivityService : IBotService
{
    private readonly ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> PendingResponses;


    public InteractivityService()
    {
        this.PendingResponses = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>();
    }


    public void AddPendingResponse(ulong cid, ulong uid)
    {
        this.PendingResponses.AddOrUpdate(
            cid,
            new ConcurrentHashSet<ulong> { uid },
            (k, v) => { v.Add(uid); return v; }
        );
    }

    public bool IsResponsePending(ulong cid, ulong uid)
        => this.PendingResponses.TryGetValue(cid, out ConcurrentHashSet<ulong>? pending) && pending.Contains(uid);

    public bool RemovePendingResponse(ulong cid, ulong uid)
    {
        if (!this.PendingResponses.TryGetValue(cid, out ConcurrentHashSet<ulong>? pending))
            return false;

        if (!pending.Contains(uid))
            return false;

        bool success = pending.TryRemove(uid);
        if (!this.PendingResponses[cid].Any())
            this.PendingResponses.TryRemove(cid, out _);
        return success;
    }
}
