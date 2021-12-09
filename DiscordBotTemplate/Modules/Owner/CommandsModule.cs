using System.Text.RegularExpressions;
using DiscordTemplateBot.Exceptions;
using DiscordTemplateBot.Extensions;
using DiscordTemplateBot.Modules.Owner.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace DiscordTemplateBot.Modules.Owner;

[Group("commands"), Hidden]
[Aliases("cmds", "cmd")]
[RequireOwner]
public sealed class CommandsModule : BotModule
{
    #region commands
    [GroupCommand]
    public Task ExecuteGroupAsync(CommandContext ctx)
        => this.ListAsync(ctx);
    #endregion

    #region commands add
    [Command("add")]
    [Aliases("register", "reg", "new", "a", "+", "+=", "<<", "<", "<-", "<=")]
    public Task AddAsync(CommandContext ctx,
                        [RemainingText, Description("C# code snippet")] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidCommandUsageException("Missing code block");

        try {
            Type? t = CSharpCompilationService.CompileCommand(code);
            if (t is null)
                throw new InvalidCommandUsageException("You need to wrap the code into a code block.");

            ctx.CommandsNext.RegisterCommands(t);
            return ctx.InfoAsync();
        } catch (Exception ex) {
            return ctx.FailAsync($"Compilation failed! Exception: {ex.GetType()}, Message: {ex.Message}");
        }
    }
    #endregion

    #region commands delete
    [Command("delete")]
    [Aliases("unregister", "remove", "rm", "del", "d", "-", "-=", ">", ">>", "->", "=>")]
    public async Task DeleteAsync(CommandContext ctx,
                           [RemainingText, Description("Command to delete")] string command)
    {
        Command cmd = ctx.CommandsNext.FindCommand(command, out _);
        if (cmd is null)
            throw new CommandFailedException("Can't find such command");
        ctx.CommandsNext.UnregisterCommands(cmd);
        await ctx.InfoAsync();
    }
    #endregion

    #region commands list
    [Command("list")]
    [Aliases("print", "show", "view", "ls", "l", "p")]
    public Task ListAsync(CommandContext ctx)
    {
        return ctx.PaginateAsync(
            "str-cmds",
            ctx.CommandsNext.GetRegisteredCommands().OrderBy(cmd => cmd.QualifiedName),
            cmd => Formatter.InlineCode(cmd.QualifiedName),
            DiscordColor.Aquamarine,
            10
        );
    }
    #endregion
}
