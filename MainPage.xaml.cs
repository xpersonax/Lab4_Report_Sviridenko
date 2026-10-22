using RailwayErp.MvvmDemo.ViewModels;

namespace RailwayErp.MvvmDemo;

public partial class MainPage : ContentPage
{
    private readonly TrainRouteListViewModel _viewModel;

    public MainPage(TrainRouteListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // LoadCommand — асинхронне читання з БД через репозиторій
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
