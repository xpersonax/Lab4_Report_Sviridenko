using System.Text.Json;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;

namespace RailwayErp.MvvmDemo.Data;

/// <summary>
/// Декоратор над локальним репозиторієм (offline-first): кожна зміна спочатку
/// зберігається в SQLite, потім потрапляє в чергу SyncQueue. Якщо мережа є —
/// черга відправляється одразу, якщо ні — чекає на з'єднання.
/// ViewModel бачить лише інтерфейс ITrainRouteRepository.
/// </summary>
public class OfflineFirstTrainRouteRepository : ITrainRouteRepository
{
    private readonly TrainRouteRepository _local;
    private readonly ISyncQueueRepository _queue;
    private readonly SyncService _sync;

    public OfflineFirstTrainRouteRepository(
        TrainRouteRepository local,
        ISyncQueueRepository queue,
        SyncService sync)
    {
        _local = local;
        _queue = queue;
        _sync = sync;
    }

    public Task<List<TrainRoute>> GetAllAsync() => _local.GetAllAsync();

    public Task<TrainRoute?> GetByIdAsync(int id) => _local.GetByIdAsync(id);

    public async Task<int> SaveAsync(TrainRoute route)
    {
        var isNew = route.Id == 0;
        var affected = await _local.SaveAsync(route);

        var stored = await _local.GetStoredAsync(route.Id);
        if (stored is not null)
        {
            await Enqueue(isNew ? SyncOperation.Insert : SyncOperation.Update, stored);
            _sync.RequestSync();
        }

        return affected;
    }

    public async Task<int> DeleteAsync(TrainRoute route)
    {
        var stored = await _local.GetStoredAsync(route.Id);
        var affected = await _local.DeleteAsync(route);

        if (stored is not null && affected > 0)
        {
            await Enqueue(SyncOperation.Delete, stored);
            _sync.RequestSync();
        }

        return affected;
    }

    private Task Enqueue(string operation, TrainRoute stored) =>
        _queue.EnqueueAsync(new SyncQueueItem
        {
            EntityType = nameof(TrainRoute),
            EntityUid = stored.Uid,
            Operation = operation,
            PayloadJson = JsonSerializer.Serialize(stored), // чутливе поле вже зашифроване
            CreatedAt = DateTime.UtcNow
        });
}
