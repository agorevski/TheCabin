using TheCabin.Maui.ViewModels;

namespace TheCabin.Maui.Views;

public partial class LoadGamePage : ContentPage
{
    private readonly LoadGameViewModel _viewModel;
    
    public LoadGamePage(LoadGameViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadSavedGamesAsync();
    }
}
