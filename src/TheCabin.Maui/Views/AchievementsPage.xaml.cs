using TheCabin.Maui.ViewModels;

namespace TheCabin.Maui.Views;

public partial class AchievementsPage : ContentPage
{
    private readonly AchievementsPageViewModel _viewModel;

    public AchievementsPage(AchievementsPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}
