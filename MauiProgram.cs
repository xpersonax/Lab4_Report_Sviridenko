using Microsoft.Extensions.Logging;
using RailwayErp.MvvmDemo.Data;
using RailwayErp.MvvmDemo.Services;
using RailwayErp.MvvmDemo.ViewModels;
using RailwayErp.MvvmDemo.Views;

namespace RailwayErp.MvvmDemo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ---- локальна БД та репозиторії (лаб. 4) ----
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "railway_erp.db3");
        builder.Services.AddSingleton(new LocalDatabase(dbPath));

        // шифрування чутливих полів
        builder.Services.AddSingleton<IKeyProvider, SecureKeyProvider>();
        builder.Services.AddSingleton<IFieldEncryptor, AesHmacFieldEncryptor>();

        // репозиторії: локальний SQLite + offline-first декоратор, який і бачить ViewModel
        builder.Services.AddSingleton<TrainRouteRepository>();
        builder.Services.AddSingleton<ISyncQueueRepository, SyncQueueRepository>();
        builder.Services.AddSingleton<ITrainRouteRepository, OfflineFirstTrainRouteRepository>();

        // мережа та синхронізація
        builder.Services.AddSingleton<INetworkStatus, NetworkStatus>();
        if (ApiSettings.UseStub)
        {
            builder.Services.AddSingleton<IRemoteApi, FakeRemoteApi>();
        }
        else
        {
            builder.Services.AddSingleton<IRemoteApi>(_ =>
                new HttpRemoteApi(new HttpClient { BaseAddress = new Uri(ApiSettings.BaseUrl) }));
        }
        builder.Services.AddSingleton<SyncService>();

        // ViewModel і сторінки
        builder.Services.AddTransient<TrainRouteListViewModel>();
        builder.Services.AddTransient<RouteEditViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<RouteEditPage>();

        Routing.RegisterRoute(nameof(RouteEditPage), typeof(RouteEditPage));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
