using DiscordTemplateBot.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DiscordTemplateBot.Modules.Owner.Extensions;

public static class DatabaseFacadeExtensions
{
    public static async Task<RelationalDataReader> ExecuteSqlQueryAsync(this DatabaseFacade databaseFacade,
                                                                        string sql,
                                                                        BotDbContext db,
                                                                        params object[] parameters)
    {

        IConcurrencyDetector concurrencyDetector = databaseFacade.GetService<IConcurrencyDetector>();

        using (concurrencyDetector.EnterCriticalSection()) {
            RawSqlCommand rawSqlCommand = databaseFacade
                .GetService<IRawSqlCommandBuilder>()
                .Build(sql, parameters);

            return await rawSqlCommand
                .RelationalCommand
                .ExecuteReaderAsync(
                    new RelationalCommandParameterObject(
                        databaseFacade.GetService<IRelationalConnection>(),
                        parameterValues: rawSqlCommand.ParameterValues,
                        readerColumns: null,
                        context: db,
                        logger: null
                    )
                );
        }
    }
}
