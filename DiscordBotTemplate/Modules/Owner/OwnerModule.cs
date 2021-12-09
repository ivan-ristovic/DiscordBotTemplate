using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Text;
using DiscordTemplateBot.Attributes;
using DiscordTemplateBot.Common;
using DiscordTemplateBot.Database;
using DiscordTemplateBot.Exceptions;
using DiscordTemplateBot.Extensions;
using DiscordTemplateBot.Modules.Owner.Common;
using DiscordTemplateBot.Modules.Owner.Extensions;
using DiscordTemplateBot.Modules.Owner.Services;
using DiscordTemplateBot.Services;
using DiscordTemplateBot.Services.Common;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace DiscordTemplateBot.Modules.Owner;

[Group("owner"), Hidden]
[Aliases("admin", "o")]
public sealed class OwnerModule : BotModule
{
    #region announce
    [Command("announce"), UsesInteractivity]
    [Aliases("ann")]
    [RequireOwner]
    public async Task AnnounceAsync(CommandContext ctx,
                                   [RemainingText, Description("Announcement")] string message)
    {
        if (!await ctx.WaitForBoolReplyAsync("Are you sure you wish to send this announcement? ```{Formatter.Strip(message)}```"))
            return;

        var emb = new DiscordEmbedBuilder() {
            Title = "An announcement from my owner!",
            Description = message,
            Color = DiscordColor.Red,
        };

        var eb = new StringBuilder();
        foreach (DiscordGuild guild in ctx.Client.Guilds.Values) {
            try {
                await guild.GetDefaultChannel().SendMessageAsync(embed: emb.Build());
            } catch {
                eb.AppendLine($"{guild.Name} | {guild.Id}");
            }
        }

        if (eb.Length > 0)
            await ctx.ImpInfoAsync($"Failed to send the announcement to the following guilds:\n{eb}");
        else
            await ctx.InfoAsync();
    }
    #endregion

    #region avatar
    [Command("avatar")]
    [Aliases("setavatar", "setbotavatar", "profilepic", "a")]
    [RequireOwner]
    public async Task SetBotAvatarAsync(CommandContext ctx,
                                       [Description("Image URL")] Uri url)
    {
        if (!await url.ContentTypeHeaderIsImageAsync(DiscordLimits.AvatarSizeLimit))
            throw new CommandFailedException($"URL must point to an image and use HTTP or HTTPS protocols and have size smaller than {DiscordLimits.AvatarSizeLimit}B");

        try {
            using MemoryStream ms = await HttpService.GetMemoryStreamAsync(url);
            await ctx.Client.UpdateCurrentUserAsync(avatar: ms);
        } catch (WebException e) {
            throw new CommandFailedException(e, "Failed to fetch the image");
        }

        await ctx.InfoAsync();
    }
    #endregion

    #region name
    [Command("name")]
    [Aliases("botname", "setbotname", "setname")]
    [RequireOwner]
    public async Task SetBotNameAsync(CommandContext ctx,
                                     [RemainingText, Description("New name")] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidCommandUsageException("Name not provided!");

        if (name.Length > DiscordLimits.NameLimit)
            throw new InvalidCommandUsageException($"Name must be shorter than {DiscordLimits.NameLimit} characters!");

        await ctx.Client.UpdateCurrentUserAsync(username: name);
        await ctx.InfoAsync();
    }
    #endregion

    #region dbquery
    [Command("dbquery"), Priority(1)]
    [Aliases("sql", "dbq", "q", "query")]
    [RequireOwner]
    public async Task DatabaseQuery(CommandContext ctx)
    {
        if (!ctx.Message.Attachments.Any())
            throw new CommandFailedException("Either write a query or attach a .sql file containing it.");

        DiscordAttachment? attachment = ctx.Message.Attachments.FirstOrDefault(att => att.FileName.EndsWith(".sql"));
        if (attachment is null)
            throw new CommandFailedException("No .sql files attached.");

        string query;
        try {
            query = await HttpService.GetStringAsync(attachment.Url).ConfigureAwait(false);
        } catch (Exception e) {
            throw new CommandFailedException(e, "Failed retrieving the message attachment");
        }

        await this.DatabaseQuery(ctx, query);
    }

    [Command("dbquery"), Priority(0)]
    public async Task DatabaseQuery(CommandContext ctx,
                                   [RemainingText, Description("SQL query")] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new InvalidCommandUsageException("Missing SQL query.");

        var res = new List<IReadOnlyDictionary<string, string>>();
        using (BotDbContext db = ctx.Services.GetRequiredService<BotDbContextBuilder>().CreateContext())
        using (RelationalDataReader dr = await db.Database.ExecuteSqlQueryAsync(query, db)) {
            DbDataReader reader = dr.DbDataReader;
            while (await reader.ReadAsync()) {
                var dict = new Dictionary<string, string>();

                for (int i = 0; i < reader.FieldCount; i++)
                    dict[reader.GetName(i)] = reader[i] is DBNull ? "NULL" : reader[i]?.ToString() ?? "NULL";

                res.Add(new ReadOnlyDictionary<string, string>(dict));
            }
        }

        if (!res.Any() || !res.First().Any()) {
            await ctx.ImpInfoAsync(Emojis.Information, "No results returned (this is OK if this was not a SELECT query)");
            return;
        }

        int maxlen = 1 + res
            .First()
            .Select(r => r.Key)
            .OrderByDescending(r => r.Length)
            .First()
            .Length;

        await ctx.PaginateAsync(
            "Results",
            res.Take(25),
            row => {
                var sb = new StringBuilder();
                foreach ((string col, string val) in row)
                    sb.Append(col).Append(new string(' ', maxlen - col.Length)).Append("| ").AppendLine(val);
                return Formatter.BlockCode(sb.ToString());
            },
            DiscordColor.Blue,
            1
        );
    }
    #endregion

    #region eval
    [Command("eval")]
    [Aliases("evaluate", "compile", "run", "e", "c", "r", "exec")]
    [RequireOwner]
    public async Task EvaluateAsync(CommandContext ctx,
                                   [RemainingText, Description("C# code snippet to evaluate")] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidCommandUsageException("You need to wrap the code into a code block.");

        Script<object>? snippet = CSharpCompilationService.Compile(code, out ImmutableArray<Diagnostic> diag, out Stopwatch compileTime);
        if (snippet is null)
            throw new InvalidCommandUsageException("You need to wrap the code into a code block.");

        var emb = new DiscordEmbedBuilder();
        if (diag.Any(d => d.Severity == DiagnosticSeverity.Error)) {
            emb.WithTitle("Evaluation failed");
            emb.WithDescription($"Compilation failed after {compileTime.ElapsedMilliseconds}ms with {diag.Length} errors.");
            emb.WithColor(DiscordColor.Red);

            foreach (Diagnostic d in diag.Take(3)) {
                FileLinePositionSpan ls = d.Location.GetLineSpan();
                emb.AddField($"Error at {ls.StartLinePosition.Line}L:{ls.StartLinePosition.Character}C", Formatter.BlockCode(d.GetMessage()));
            }

            if (diag.Length > 3)
                emb.AddField("...", $"**{diag.Length - 3}** errors not displayed.");

            await ctx.RespondAsync(emb.Build());
            return;
        }

        Exception? exc = null;
        ScriptState<object>? res = null;
        var runTime = Stopwatch.StartNew();
        try {
            res = await snippet.RunAsync(new EvaluationEnvironment(ctx));
        } catch (Exception e) {
            exc = e;
        }
        runTime.Stop();

        if (exc is { } || res is null) {
            emb.WithTitle("Program run failed");
            emb.WithDescription("Execution failed after {runTime.ElapsedMilliseconds}ms with `{exc?.GetType()}`: ```{exc?.Message}```");
            emb.WithColor(DiscordColor.Red);
        } else {
            emb.WithTitle("Evaluation successful!");
            emb.WithColor(DiscordColor.Green);
            if (res.ReturnValue is { }) {
                emb.AddField("Result", res.ReturnValue.ToString(), false);
                emb.AddField("Result type", res.ReturnValue.GetType().ToString(), true);
            } else {
                emb.AddField("Result", "No result returned", inline: true);
            }
            emb.AddField("Compilation time (ms)", compileTime.ElapsedMilliseconds.ToString(), true);
            emb.AddField("Evaluation time (ms)", runTime.ElapsedMilliseconds.ToString(), true);
        }

        await ctx.RespondAsync(emb.Build());
    }
    #endregion

    #region leaveguilds
    [Command("leaveguilds"), Priority(1)]
    [Aliases("leave", "gtfo")]
    [RequireOwner]
    public Task LeaveGuildsAsync(CommandContext ctx,
                                [Description("Guilds to leave")] params DiscordGuild[] guilds)
        => this.LeaveGuildsAsync(ctx, guilds.Select(g => g.Id).ToArray());

    [Command("leaveguilds"), Priority(0)]
    public async Task LeaveGuildsAsync(CommandContext ctx,
                                      [Description("Guilds to leave")] params ulong[] gids)
    {
        if (gids is null || !gids.Any()) {
            await ctx.Guild.LeaveAsync();
            return;
        }

        var eb = new StringBuilder();
        foreach (ulong gid in gids) {
            try {
                if (ctx.Client.Guilds.TryGetValue(gid, out DiscordGuild? guild))
                    await guild.LeaveAsync();
                else
                    eb.AppendLine($"Error: Failed to leave the guild with ID: `{gid}`!");
            } catch {
                eb.AppendLine($"Warning: I am not a member of the guild with ID: `{gid}`!");
            }
        }

        if (ctx.Guild is { } && !gids.Contains(ctx.Guild.Id)) {
            if (eb.Length > 0)
                await ctx.FailAsync("Action finished with following warnings/errors:\n\n{eb}");
            else
                await ctx.InfoAsync();
        } else {
            await ctx.InfoAsync();
        }
    }
    #endregion

    #region log
    [Command("log"), Priority(1), UsesInteractivity]
    [Aliases("getlog", "remark", "rem")]
    [RequireOwner]
    public async Task LogAsync(CommandContext ctx,
                              [Description("Bypass log config?")] bool bypassConfig = false)
    {
        BotConfig cfg = ctx.Services.GetRequiredService<BotConfigService>().CurrentConfiguration;

        if (!bypassConfig && !cfg.LogToFile)
            throw new CommandFailedException("Logging is disabled");

        var fi = new FileInfo(cfg.LogPath);
        if (fi.Exists) {
            fi = new FileInfo(cfg.LogPath);
            if (fi.Length > DiscordLimits.AttachmentLimit)
                throw new CommandFailedException($"Log file {fi.Name} is too large to send ({fi.Length}B)!");
        } else {
            DirectoryInfo? di = fi.Directory;
            if (di?.Exists ?? false) {
                var fis = di.GetFiles()
                    .OrderByDescending(fi => fi.CreationTime)
                    .Select((fi, i) => (fi, i))
                    .ToDictionary(tup => tup.i, tup => tup.fi)
                    ;
                if (!fis.Any())
                    throw new CommandFailedException($"Cannot find any log files on path: {cfg.LogPath}");

                await ctx.PaginateAsync(
                    "Select log file to display:",
                    fis,
                    kvp => Formatter.InlineCode($"{kvp.Key:D3}: {kvp.Value.Name}"),
                    DiscordColor.Gold
                );

                int? index = await ctx.Client.GetInteractivity().WaitForOptionReplyAsync(ctx, fis.Count);
                if (index is null)
                    return;

                if (!fis.TryGetValue(index.Value, out fi))
                    throw new CommandFailedException($"Cannot find such log file on path: {cfg.LogPath}");
            } else {
                throw new CommandFailedException($"Cannot find log path: {cfg.LogPath}");
            }
        }

        using FileStream? fs = fi.OpenRead();
        await ctx.RespondAsync(new DiscordMessageBuilder().WithFile(fs));
    }

    [Command("log"), Priority(0)]
    public Task LogAsync(CommandContext ctx,
                        [Description("Log level")] LogEventLevel level,
                        [RemainingText, Description("Log remark")] string text)
    {
        Log.Write(level, "{LogRemark}", text);
        return ctx.InfoAsync();
    }
    #endregion

    #region sendmessage
    [Command("sendmessage")]
    [Aliases("send", "s")]
    [RequirePrivilegedUser]
    public async Task SendAsync(CommandContext ctx,
                               [Description("(u/c)")] string desc,
                               [Description("ID")] ulong xid,
                               [RemainingText, Description("Message")] string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new InvalidCommandUsageException("Message missing");

        if (string.Equals(desc, "u", StringComparison.InvariantCultureIgnoreCase)) {
            DiscordDmChannel? dm = await ctx.Client.CreateDmChannelAsync(xid);
            if (dm is null)
                throw new CommandFailedException("Failed to create DM channel");
            await dm.SendMessageAsync(message);
        } else if (string.Equals(desc, "c", StringComparison.InvariantCultureIgnoreCase)) {
            DiscordChannel channel = await ctx.Client.GetChannelAsync(xid);
            await channel.SendMessageAsync(message);
        } else {
            throw new InvalidCommandUsageException("Enter either `u` or `c` as first argument");
        }

        await ctx.InfoAsync();
    }
    #endregion

    #region shutdown
    [Command("shutdown"), Priority(1)]
    [Aliases("disable", "poweroff", "exit", "quit")]
    [RequirePrivilegedUser]
    public Task ExitAsync(CommandContext _,
                         [Description("Time until exit")] TimeSpan timespan,
                         [Description("Exit code")] int exitCode = 0)
        => Program.Stop(exitCode, timespan);

    [Command("shutdown"), Priority(0)]
    public Task ExitAsync(CommandContext _,
                         [Description("Exit code")] int exitCode = 0)
        => Program.Stop(exitCode);
    #endregion

    #region sudo
    [Command("sudo")]
    [Aliases("execas", "as")]
    [RequireGuild, RequirePrivilegedUser]
    public async Task SudoAsync(CommandContext ctx,
                               [Description("Member to execute as")] DiscordMember member,
                               [RemainingText, Description("Full command name")] string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new InvalidCommandUsageException();

        Command? cmd = ctx.CommandsNext.FindCommand(command, out string args);
        if (cmd is null)
            throw new CommandFailedException("Can't find such command. Are you sure you wrote the full command name?");
        if (cmd.ExecutionChecks.Any(c => c is RequireOwnerAttribute or RequirePrivilegedUserAttribute))
            throw new CommandFailedException("Cannot sudo privileged commands");
        CommandContext fctx = ctx.CommandsNext.CreateFakeContext(member, ctx.Channel, command, ctx.Prefix, cmd, args);
        if ((await cmd.RunChecksAsync(fctx, false)).Any())
            throw new CommandFailedException("Command checks failed");

        await ctx.CommandsNext.ExecuteCommandAsync(fctx);
    }
    #endregion

    #region toggleignore
    [Command("toggleignore")]
    [Aliases("ti")]
    [RequirePrivilegedUser]
    public Task ToggleIgnoreAsync(CommandContext ctx)
    {
        BotActivityService bas = ctx.Services.GetRequiredService<BotActivityService>();
        bool ignoreEnabled = bas.ToggleListeningStatus();
        return ctx.InfoAsync();
    }
    #endregion

    #region restart
    [Command("restart")]
    [Aliases("reboot")]
    [RequirePrivilegedUser]
    public Task RestartAsync(CommandContext ctx)
        => this.ExitAsync(ctx, 100);
    #endregion

    #region update
    [Command("update")]
    [RequireOwner]
    public Task UpdateAsync(CommandContext ctx)
        => this.ExitAsync(ctx, 101);
    #endregion

    #region uptime
    [Command("uptime")]
    [RequirePrivilegedUser]
    public Task UptimeAsync(CommandContext ctx)
    {
        BotActivityService bas = ctx.Services.GetRequiredService<BotActivityService>();
        TimeSpan processUptime = bas.UptimeInformation.ProgramUptime;
        TimeSpan socketUptime = bas.UptimeInformation.SocketUptime;

        var emb = new DiscordEmbedBuilder {
            Title = "Uptime information",
            Description = $"{Program.ApplicationName} {Program.ApplicationVersion}",
            Color = DiscordColor.Gold,
        };
        emb.AddField("Bot uptime", processUptime.ToString(@"dd\.hh\:mm\:ss"), inline: true);
        emb.AddField("Socket uptime", socketUptime.ToString(@"dd\.hh\:mm\:ss"), inline: true);

        return ctx.RespondAsync(emb.Build());
    }
    #endregion
}
