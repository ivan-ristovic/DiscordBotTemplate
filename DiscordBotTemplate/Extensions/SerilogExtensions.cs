using DiscordTemplateBot.Services.Common;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DiscordTemplateBot.Extensions;

internal static class LogExt
{
    public static Logger CreateLogger(BotConfig cfg)
    {
        string template = cfg.CustomLogTemplate
            ?? "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}] [{Application}] [{Level:u3}] [T{ThreadId:d2}] ({ShardId}) {Message:l}{NewLine}{Exception}";

        LoggerConfiguration lcfg = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.With<Enrichers.ThreadIdEnricher>()
            .Enrich.With<Enrichers.ApplicationNameEnricher>()
            .MinimumLevel.Is(cfg.LogLevel)
            .WriteTo.Console(outputTemplate: template)
            ;

        if (cfg.LogToFile) {
            lcfg = lcfg.WriteTo.File(
                cfg.LogPath,
                cfg.LogLevel,
                outputTemplate: template,
                rollingInterval: cfg.RollingInterval,
                buffered: cfg.UseBufferedFileLogger,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: cfg.MaxLogFiles
            );
        }

        return lcfg.CreateLogger();
    }
}

internal sealed class Enrichers
{
    public sealed class ThreadIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            => logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadId", Thread.CurrentThread.ManagedThreadId));
    }

    public sealed class ApplicationNameEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            => logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Application", Program.ApplicationName));
    }
}
