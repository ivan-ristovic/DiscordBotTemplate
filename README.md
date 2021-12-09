# C# + DSharpPlus Discord Bot Template
[![Issues](https://img.shields.io/github/issues/ivan-ristovic/DiscordBotTemplate.svg)](https://github.com/ivan-ristovic/DiscordBotTemplate/issues)
[![Discord Server](https://discord.com/api/guilds/794671727291531274/embed.png)](https://discord.gg/z7KZGQQxRz)

## About
This is a semi-advanced Discord bot template written in C# (.NET 6) using [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus), made for developers that wish to start from a service-oriented and auto-extensible bot, rather than from scratch. The bot is backed by a database of your choice (SQLite, PostgreSQL), and it is easy to add more database providers to suit your needs. Logging is done via Serilog, and the bot configuration file has various switches that modify the logging procedure (log level, file logging, file rolling intervals etc.). The bot comes along with a owner-only module set, supplying basic commands such as avatar/name/status changes, code execution, command manipulation etc.

To see a more complex bot that inspired the creation of this example, check out [TheGodfather](https://github.com/ivan-ristovic/the-godfather)!

## Template outline
  - `Attributes` - Common attributes used throughout the project, mainly as command permission modifiers
  - `Common` - Common classes
    - `Common/Collections` - Custom useful data structures
    - `Common/Converters` - Command argument converters, new converters are automatically registered by the bot
  - `Database` - Database-related controllers and context builders
    - `Database/Models` - Database entities
  - `EventListeners` - Discord event listeners, automatically registered by the bot if marked with an appropriate attribute
  - `Exceptions` - Exception types used by the bot
  - `Extensions` - Useful extensions
  - `Modules` - Bot command modules, automatically registered by the bot
  - `Services` - Abstract and concrete services used by the bot
