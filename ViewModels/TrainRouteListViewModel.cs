using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RailwayErp.MvvmDemo.Data;
using RailwayErp.MvvmDemo.Models;
using RailwayErp.MvvmDemo.Services;
using RailwayErp.MvvmDemo.Views;

namespace RailwayErp.MvvmDemo.ViewModels;

public partial class TrainRouteListViewModel : ObservableObject
{
    private readonly ITrainRouteRepository _repository;
    private readonly ISyncQueueRepository _queue;
    private readonly SyncService _sync;
    private readonly INetworkStatus _network;
    private readonly IRemoteApi _api;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public ObservableCollection<TrainRoute> Routes { get; } = new();

    [ObservableProperty]
    private bool _isOfflineMode;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _lastSyncText = string.Empty;

    /// <summary>Кнопка демонстрації конфлікту доступна лише з вбудованою заглушкою сервера.</summary>
    public bool CanSimulateServer => _api is FakeRemoteApi;

    public TrainRouteListViewModel(
        ITrainRouteRepository repository,
        ISyncQueueRepository queue,
        SyncService sync,
        INetworkStatus network,
        IRemoteApi api)
    {
        _repository = repository;
        _queue = queue;
        _sync = sync;
        _network = network;
        _api = api;

        _network.Changed += (_, _) =>
            MainThread.BeginInvokeOnMainThread(async () => await UpdateStatusAsync());

        _sync.SyncCompleted += (_, report) =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                LastSyncText = $"Синхронізація: надіслано {report.Sent}, конфліктів {report.Conflicts}";
                await LoadAsync();
            });
    }

    partial void OnIsOfflineModeChanged(bool value) => _network.ForceOffline = value;

    [RelayCommand]
    private async Task LoadAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            var items = await _repository.GetAllAsync();

            Routes.Clear();
            foreach (var route in items)
                Routes.Add(route);

            await UpdateStatusAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(nameof(RouteEditPage));

    [RelayCommand]
    private Task EditAsync(TrainRoute? route) =>
        route is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync($"{nameof(RouteEditPage)}?Id={route.Id}");

    [RelayCommand]
    private async Task DeleteAsync(TrainRoute? route)
    {
        if (route is null)
            return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Видалення", $"Видалити маршрут «{route.Name}»?", "Так", "Ні");
        if (!confirmed)
            return;

        await _repository.DeleteAsync(route);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        var report = await _sync.SyncAsync();

        LastSyncText = report.WasOffline
            ? "Немає мережі — зміни залишаються в черзі"
            : $"Синхронізація: надіслано {report.Sent}, конфліктів {report.Conflicts}";

        await UpdateStatusAsync();
    }

    [RelayCommand]
    private async Task SimulateServerChangeAsync()
    {
        if (_api is not FakeRemoteApi fake)
            return;

        var changed = Routes.Count(route => fake.SimulateServerChange(route.Uid));
        LastSyncText = changed > 0
            ? $"Імітація: на сервері змінено записів — {changed}"
            : "Імітація: на сервері ще немає записів (спочатку синхронізуйте маршрут)";

        await Task.CompletedTask;
    }

    private async Task UpdateStatusAsync()
    {
        var pending = await _queue.CountAsync();
        StatusText = $"{(_network.IsOnline ? "Онлайн" : "Офлайн")}  |  У черзі синхронізації: {pending}";
    }
}
