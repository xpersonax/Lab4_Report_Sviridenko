using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Data;

public interface ISyncQueueRepository
{
    Task EnqueueAsync(SyncQueueItem item);
    Task<List<SyncQueueItem>> GetPendingAsync();
    Task RemoveAsync(SyncQueueItem item);
    Task<int> CountAsync();
}

public class SyncQueueRepository : ISyncQueueRepository
{
    private readonly LocalDatabase _database;

    public SyncQueueRepository(LocalDatabase database) => _database = database;

    public async Task EnqueueAsync(SyncQueueItem item)
    {
        var db = await _database.GetAsync();
        await db.InsertAsync(item);
    }

    // FIFO: зміни відправляються в тому порядку, в якому були зроблені
    public async Task<List<SyncQueueItem>> GetPendingAsync()
    {
        var db = await _database.GetAsync();
        return await db.Table<SyncQueueItem>().OrderBy(i => i.Id).ToListAsync();
    }

    public async Task RemoveAsync(SyncQueueItem item)
    {
        var db = await _database.GetAsync();
        await db.DeleteAsync(item);
    }

    public async Task<int> CountAsync()
    {
        var db = await _database.GetAsync();
        return await db.Table<SyncQueueItem>().CountAsync();
    }
}
