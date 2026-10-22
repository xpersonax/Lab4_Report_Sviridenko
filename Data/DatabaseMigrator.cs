using SQLite;

namespace RailwayErp.MvvmDemo.Data;

/// <summary>
/// Версіонування схеми БД через PRAGMA user_version.
/// Кожен крок застосовується один раз, послідовно, в одній транзакції.
/// </summary>
public static class DatabaseMigrator
{
    public const int CurrentVersion = 3;

    private static readonly (int Version, string[] Statements)[] Steps =
    {
        // v1: початкова схема з лабораторної (TrainRoute)
        (1, new[]
        {
            @"CREATE TABLE IF NOT EXISTS TrainRoute (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name VARCHAR(100) NOT NULL,
                Origin VARCHAR(100) NOT NULL,
                Destination VARCHAR(100) NOT NULL,
                DepartureTime BIGINT NOT NULL)"
        }),

        // v2: дані машиніста (шифроване поле), глобальний Uid і час зміни для синхронізації
        (2, new[]
        {
            "ALTER TABLE TrainRoute ADD COLUMN DriverName VARCHAR(100)",
            "ALTER TABLE TrainRoute ADD COLUMN DriverDocumentNumber VARCHAR(512)",
            "ALTER TABLE TrainRoute ADD COLUMN Uid VARCHAR(36)",
            "ALTER TABLE TrainRoute ADD COLUMN UpdatedAt BIGINT NOT NULL DEFAULT 0",
            // існуючим рядкам (якщо є) призначаємо GUID у текстовому форматі
            @"UPDATE TrainRoute SET Uid =
                lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' ||
                substr(lower(hex(randomblob(2))), 2) || '-' ||
                substr('89ab', abs(random()) % 4 + 1, 1) ||
                substr(lower(hex(randomblob(2))), 2) || '-' || lower(hex(randomblob(6)))
              WHERE Uid IS NULL",
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_TrainRoute_Uid ON TrainRoute (Uid)"
        }),

        // v3: черга відкладених змін для офлайн-синхронізації
        (3, new[]
        {
            @"CREATE TABLE IF NOT EXISTS SyncQueue (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                EntityType VARCHAR(50) NOT NULL,
                EntityUid VARCHAR(36) NOT NULL,
                Operation VARCHAR(10) NOT NULL,
                PayloadJson VARCHAR NOT NULL,
                CreatedAt BIGINT NOT NULL)"
        })
    };

    public static async Task MigrateAsync(SQLiteAsyncConnection db)
    {
        var current = await db.ExecuteScalarAsync<int>("PRAGMA user_version");

        foreach (var step in Steps.Where(s => s.Version > current).OrderBy(s => s.Version))
        {
            await db.RunInTransactionAsync(connection =>
            {
                foreach (var sql in step.Statements)
                    connection.Execute(sql);
            });

            await db.ExecuteAsync($"PRAGMA user_version = {step.Version}");
        }
    }
}
