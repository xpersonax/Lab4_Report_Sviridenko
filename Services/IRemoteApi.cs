using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services;

public enum PushStatus
{
    /// <summary>Сервер прийняв зміну.</summary>
    Ok,

    /// <summary>На сервері новіша версія — спрацювала стратегія last write wins.</summary>
    Conflict,

    /// <summary>Тимчасова помилка (немає зв'язку, 5xx) — спробувати пізніше.</summary>
    Failed
}

public record RemotePushResult(PushStatus Status, TrainRoute? ServerVersion = null, string? Message = null);

public interface IRemoteApi
{
    Task<RemotePushResult> PushAsync(SyncQueueItem item, CancellationToken ct = default);
}

public static class ApiSettings
{
    /// <summary>true — працюємо із вбудованою заглушкою сервера (FakeRemoteApi).</summary>
    public const bool UseStub = true;

    /// <summary>Адреса реального REST API (використовується, якщо UseStub = false).</summary>
    public const string BaseUrl = "https://localhost:5001/";
}
