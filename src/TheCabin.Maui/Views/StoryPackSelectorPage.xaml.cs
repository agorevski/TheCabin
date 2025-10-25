using TheCabin.Maui.ViewModels;

namespace TheCabin.Maui.Views;

public partial class StoryPackSelectorPage : ContentPage
{
    private readonly StoryPackSelectorViewModel _viewModel;
    
    // Parameterless constructor for Shell navigation
    public StoryPackSelectorPage() : this(GetViewModel())
    {
    }
    
    public StoryPackSelectorPage(StoryPackSelectorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
    
    private static StoryPackSelectorViewModel GetViewModel()
    {
        // Get the ViewModel from the service provider
        return Application.Current?.Handler?.MauiContext?.Services
            .GetRequiredService<StoryPackSelectorViewModel>()
            ?? throw new InvalidOperationException("Unable to resolve StoryPackSelectorViewModel");
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadPacksAsync();
    }
}
