using SQLite;

namespace RailwayErp.MvvmDemo.Data;

/// <summary>
/// Єдина точка доступу до SQLiteAsyncConnection. Ледача ініціалізація:
/// при першому зверненні відкриває БД і застосовує міграції.
/// </summary>
public class LocalDatabase
{
    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private readonly string _dbPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private SQLiteAsyncConnection? _connection;

    public LocalDatabase(string dbPath) => _dbPath = dbPath;

    public string DatabasePath => _dbPath;

    public async Task<SQLiteAsyncConnection> GetAsync()
    {
        if (_connection is not null)
            return _connection;

        await _lock.WaitAsync();
        try
        {
            if (_connection is null)
            {
                var connection = new SQLiteAsyncConnection(_dbPath, Flags);
                await DatabaseMigrator.MigrateAsync(connection);
                _connection = connection;
            }

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }
}
