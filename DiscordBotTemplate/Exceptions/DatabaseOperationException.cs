namespace DiscordTemplateBot.Exceptions;

public class DatabaseOperationException : Exception
{
    public DatabaseOperationException()
        : base("Database operation failed") { }

    public DatabaseOperationException(string msg)
        : base(msg) { }

    public DatabaseOperationException(Exception inner, string msg)
        : base(msg, inner) { }
}
