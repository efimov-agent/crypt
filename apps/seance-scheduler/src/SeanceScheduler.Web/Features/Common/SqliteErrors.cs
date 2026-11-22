using Microsoft.Data.Sqlite;

namespace SeanceScheduler.Web.Features.Common;

public static class SqliteErrors
{
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;

    /// <summary>
    /// True when the database stayed locked by another writer past the command timeout
    /// (SQLITE_BUSY / SQLITE_LOCKED), possibly wrapped by EF Core. This is a transient server condition.
    /// </summary>
    public static bool IsBusy(Exception? exception)
    {
        for (var ex = exception; ex is not null; ex = ex.InnerException)
        {
            if (ex is SqliteException { SqliteErrorCode: SqliteBusy or SqliteLocked })
            {
                return true;
            }
        }

        return false;
    }
}
