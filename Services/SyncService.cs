using RailwayErp.MvvmDemo.Data;

namespace RailwayErp.MvvmDemo.Services;

public record SyncReport(int Sent, int Conflicts, int Remaining, bool WasOffline);

/// <summary>
/// Сервіс синхронізації: послідовно (FIFO) вичитує чергу SyncQueue, відправляє зміни
/// на сервер і видаляє оброблені записи. Спрацьовує автоматично при появі мережі.
/// Конфлікти вирішуються за стратегією last write wins.
/// </summary>
public class SyncService
{
    private readonly ISyncQueueRepository _queue;
    private readonly TrainRouteRepository _local;
    private readonly IRemoteApi _api;
    private readonly INetworkStatus _network;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SyncService(
        ISyncQueueRepository queue,
        TrainRouteRepository local,
        IRemoteApi api,
        INetworkStatus network)
    {
        _queue = queue;
        _local = local;
        _api = api;
        _network = network;

        _network.Changed += (_, _) =>
        {
            if (_network.IsOnline)
                RequestSync();
        };
    }

    public event EventHandler<SyncReport>? SyncCompleted;

    /// <summary>Запускає синхронізацію у фоні, не блокуючи UI і не кидаючи винятків.</summary>
    public void RequestSync() => _ = Task.Run(async () =>
    {
        try
        {
            await SyncAsync();
        }
        catch (Exception)
        {
            // помилка не повинна «вбивати» застосунок; записи залишаються в черзі
        }
    });

    public async Task<SyncReport> SyncAsync(CancellationToken ct = default)
    {
        if (!_network.IsOnline)
            return new SyncReport(0, 0, await _queue.CountAsync(), true);

        await _gate.WaitAsync(ct);
        int sent = 0, conflicts = 0;
        try
        {
            foreach (var item in await _queue.GetPendingAsync())
            {
                if (!_network.IsOnline)
                    break;

                RemotePushResult result;
                try
                {
                    result = await _api.PushAsync(item, ct);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    break; // немає зв'язку — залишаємо запис і решту черги на потім
                }

                if (result.Status == PushStatus.Failed)
                    break; // зберігаємо порядок змін

                if (result.Status == PushStatus.Conflict)
                {
                    conflicts++;
                    // last write wins: на сервері новіша версія → перезаписуємо локальну
                    if (result.ServerVersion is not null)
                        await _local.ApplyServerVersionAsync(result.ServerVersion);
                }
                else
                {
                    sent++;
                }

                await _queue.RemoveAsync(item);
            }
        }
        finally
        {
            _gate.Release();
        }

        var report = new SyncReport(sent, conflicts, await _queue.CountAsync(), false);
        SyncCompleted?.Invoke(this, report);
        return report;
    }
}
