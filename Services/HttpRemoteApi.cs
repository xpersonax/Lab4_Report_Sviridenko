using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services;

/// <summary>
/// Реальний REST-клієнт (HttpClient). Очікуваний контракт сервера:
/// POST api/routes, PUT api/routes/{uid}, DELETE api/routes/{uid}?updatedAt={ticks};
/// 409 Conflict повертає у тілі поточну версію запису із сервера.
/// </summary>
public class HttpRemoteApi : IRemoteApi
{
    private readonly HttpClient _http;

    public HttpRemoteApi(HttpClient http) => _http = http;

    public async Task<RemotePushResult> PushAsync(SyncQueueItem item, CancellationToken ct = default)
    {
        var route = JsonSerializer.Deserialize<TrainRoute>(item.PayloadJson)!;

        HttpResponseMessage response;
        switch (item.Operation)
        {
            case SyncOperation.Insert:
                response = await _http.PostAsJsonAsync("api/routes", route, ct);
                break;
            case SyncOperation.Update:
                response = await _http.PutAsJsonAsync($"api/routes/{item.EntityUid}", route, ct);
                break;
            case SyncOperation.Delete:
                response = await _http.DeleteAsync($"api/routes/{item.EntityUid}?updatedAt={route.UpdatedAt.Ticks}", ct);
                break;
            default:
                return new RemotePushResult(PushStatus.Failed, null, $"Невідома операція: {item.Operation}");
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
                return new RemotePushResult(PushStatus.Ok);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var serverVersion = await response.Content.ReadFromJsonAsync<TrainRoute>(cancellationToken: ct);
                return new RemotePushResult(PushStatus.Conflict, serverVersion, "Конфлікт: перемагає новіша версія.");
            }

            return new RemotePushResult(PushStatus.Failed, null, $"HTTP {(int)response.StatusCode}");
        }
    }
}
