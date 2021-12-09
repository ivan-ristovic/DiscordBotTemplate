namespace DiscordTemplateBot.Exceptions;

public class CommandFailedException : Exception
{
    public CommandFailedException()
        : base("Command failed") { }

    public CommandFailedException(string msg)
        : base(msg) { }

    public CommandFailedException(Exception inner, string msg)
        : base(msg, inner) { }
}
