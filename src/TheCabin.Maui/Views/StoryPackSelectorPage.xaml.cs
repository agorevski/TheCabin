using TheCabin.Maui.ViewModels;

namespace TheCabin.Maui.Views;

public partial class StoryPackSelectorPage : ContentPage
{
    private readonly StoryPackSelectorViewModel _viewModel;
    
    public StoryPackSelectorPage(StoryPackSelectorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadPacksAsync();
    }
}
