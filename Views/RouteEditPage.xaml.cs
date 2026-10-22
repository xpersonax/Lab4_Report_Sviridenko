using RailwayErp.MvvmDemo.ViewModels;

namespace RailwayErp.MvvmDemo.Views;

public partial class RouteEditPage : ContentPage
{
    public RouteEditPage(RouteEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
