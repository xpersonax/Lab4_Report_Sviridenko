using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo.Data;

/// <summary>
/// Локальний репозиторій SQLite. Прозоро шифрує DriverDocumentNumber при записі
/// та розшифровує при читанні, тому ViewModel працює лише з відкритим текстом.
/// </summary>
public class TrainRouteRepository : ITrainRouteRepository
{
    private readonly LocalDatabase _database;
    private readonly IFieldEncryptor _encryptor;

    public TrainRouteRepository(LocalDatabase database, IFieldEncryptor encryptor)
    {
        _database = database;
        _encryptor = encryptor;
    }

    public async Task<List<TrainRoute>> GetAllAsync()
    {
        var db = await _database.GetAsync();
        var rows = await db.Table<TrainRoute>().OrderBy(r => r.DepartureTime).ToListAsync();

        foreach (var row in rows)
            row.DriverDocumentNumber = await _encryptor.DecryptAsync(row.DriverDocumentNumber);

        return rows;
    }

    public async Task<TrainRoute?> GetByIdAsync(int id)
    {
        var db = await _database.GetAsync();
        var row = await db.FindAsync<TrainRoute>(id);

        if (row is not null)
            row.DriverDocumentNumber = await _encryptor.DecryptAsync(row.DriverDocumentNumber);

        return row;
    }

    public async Task<int> SaveAsync(TrainRoute route)
    {
        var db = await _database.GetAsync();

        if (route.Uid == Guid.Empty)
            route.Uid = Guid.NewGuid();
        route.UpdatedAt = DateTime.UtcNow;

        // у БД потрапляє копія із шифротекстом; оригінал (відкритий текст) не змінюємо
        var row = route.Clone();
        row.DriverDocumentNumber = await _encryptor.EncryptAsync(route.DriverDocumentNumber);

        int affected;
        if (route.Id == 0)
        {
            affected = await db.InsertAsync(row);
            route.Id = row.Id;
        }
        else
        {
            affected = await db.UpdateAsync(row);
        }

        return affected;
    }

    public async Task<int> DeleteAsync(TrainRoute route)
    {
        var db = await _database.GetAsync();
        return await db.DeleteAsync(route);
    }

    // ---- службові методи для синхронізації (працюють із «сирим» рядком БД) ----

    /// <summary>Рядок так, як він збережений у БД (із шифротекстом).</summary>
    public async Task<TrainRoute?> GetStoredAsync(int id)
    {
        var db = await _database.GetAsync();
        return await db.FindAsync<TrainRoute>(id);
    }

    /// <summary>Застосовує версію із сервера (шифротекст) до локальної БД за Uid.</summary>
    public async Task ApplyServerVersionAsync(TrainRoute serverVersion)
    {
        var db = await _database.GetAsync();
        var existing = await db.Table<TrainRoute>()
            .Where(r => r.Uid == serverVersion.Uid)
            .FirstOrDefaultAsync();

        serverVersion.Id = existing?.Id ?? 0;

        if (existing is null)
            await db.InsertAsync(serverVersion);
        else
            await db.UpdateAsync(serverVersion);
    }
}
