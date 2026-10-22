using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RailwayErp.MvvmDemo.Data;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.ViewModels;

/// <summary>Додавання та редагування маршруту (Id передається через параметр Shell-навігації).</summary>
public partial class RouteEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly ITrainRouteRepository _repository;
    private TrainRoute? _editing;

    [ObservableProperty] private string _title = "Новий маршрут";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _origin = string.Empty;
    [ObservableProperty] private string _destination = string.Empty;
    [ObservableProperty] private DateTime _departureDate = DateTime.Today;
    [ObservableProperty] private TimeSpan _departureTimeOfDay = new(8, 0, 0);
    [ObservableProperty] private string _driverName = string.Empty;
    [ObservableProperty] private string _driverDocumentNumber = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public RouteEditViewModel(ITrainRouteRepository repository) => _repository = repository;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Id", out var raw) &&
            int.TryParse(raw?.ToString(), out var id) && id != 0)
        {
            _ = LoadAsync(id);
        }
    }

    private async Task LoadAsync(int id)
    {
        var route = await _repository.GetByIdAsync(id);
        if (route is null)
            return;

        _editing = route;
        Title = "Редагування маршруту";
        Name = route.Name;
        Origin = route.Origin;
        Destination = route.Destination;
        DepartureDate = route.DepartureTime.Date;
        DepartureTimeOfDay = route.DepartureTime.TimeOfDay;
        DriverName = route.DriverName ?? string.Empty;
        DriverDocumentNumber = route.DriverDocumentNumber ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) ||
            string.IsNullOrWhiteSpace(Origin) ||
            string.IsNullOrWhiteSpace(Destination))
        {
            ErrorMessage = "Заповніть назву, пункт відправлення та пункт призначення.";
            return;
        }

        var route = _editing ?? new TrainRoute();
        route.Name = Name.Trim();
        route.Origin = Origin.Trim();
        route.Destination = Destination.Trim();
        route.DepartureTime = DepartureDate.Date + DepartureTimeOfDay;
        route.DriverName = string.IsNullOrWhiteSpace(DriverName) ? null : DriverName.Trim();
        route.DriverDocumentNumber = string.IsNullOrWhiteSpace(DriverDocumentNumber)
            ? null
            : DriverDocumentNumber.Trim();

        await _repository.SaveAsync(route);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
