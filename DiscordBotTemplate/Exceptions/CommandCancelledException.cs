namespace DiscordTemplateBot.Exceptions;

public sealed class CommandCancelledException : Exception
{
    public CommandCancelledException()
        : base("Command cancelled") { }

    public CommandCancelledException(string message)
        : base(message) { }

    public CommandCancelledException(string message, Exception inner)
        : base(message, inner) { }
}
