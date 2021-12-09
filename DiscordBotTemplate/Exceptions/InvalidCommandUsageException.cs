namespace DiscordTemplateBot.Exceptions;

public class InvalidCommandUsageException : Exception
{
    public InvalidCommandUsageException()
        : base("Invalid command usage") { }

    public InvalidCommandUsageException(string msg)
        : base(msg) { }

    public InvalidCommandUsageException(Exception inner, string msg)
        : base(msg, inner) { }
}
