using System.Collections.Concurrent;
using System.Text.Json;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services;

/// <summary>
/// Локальна заглушка серверного API (зберігає дані в пам'яті). Реалізує ту саму
/// стратегію вирішення конфліктів, що й справжній сервер: last write wins за UpdatedAt.
/// </summary>
public class FakeRemoteApi : IRemoteApi
{
    private readonly ConcurrentDictionary<Guid, TrainRoute> _server = new();
    private readonly object _sync = new();

    public IReadOnlyCollection<TrainRoute> ServerState => _server.Values.ToList();

    public async Task<RemotePushResult> PushAsync(SyncQueueItem item, CancellationToken ct = default)
    {
        await Task.Delay(250, ct); // імітація затримки мережі

        var incoming = JsonSerializer.Deserialize<TrainRoute>(item.PayloadJson)!;

        lock (_sync)
        {
            if (_server.TryGetValue(item.EntityUid, out var existing) &&
                existing.UpdatedAt > incoming.UpdatedAt)
            {
                return new RemotePushResult(
                    PushStatus.Conflict,
                    existing.Clone(),
                    "На сервері новіша версія запису (last write wins).");
            }

            if (item.Operation == SyncOperation.Delete)
                _server.TryRemove(item.EntityUid, out _);
            else
                _server[item.EntityUid] = incoming;

            return new RemotePushResult(PushStatus.Ok);
        }
    }

    /// <summary>
    /// Демонстрація конфлікту: «хтось інший» змінив запис на сервері зараз,
    /// тож він новіший за будь-яку локальну зміну, зроблену раніше в офлайні.
    /// </summary>
    public bool SimulateServerChange(Guid uid)
    {
        lock (_sync)
        {
            if (!_server.TryGetValue(uid, out var record))
                return false;

            record.Name += " (змінено на сервері)";
            record.UpdatedAt = DateTime.UtcNow;
            return true;
        }
    }
}
