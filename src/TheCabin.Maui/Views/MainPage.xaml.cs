using TheCabin.Maui.ViewModels;

namespace TheCabin.Maui.Views;

public partial class MainPage : ContentPage, IQueryAttributable
{
    private readonly MainViewModel _viewModel;
    private bool _isInitialized;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (!_isInitialized)
        {
            await _viewModel.InitializeAsync();
            _isInitialized = true;
        }
    }
    
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("SelectedPackId"))
        {
            // Set flag immediately to prevent OnAppearing from initializing
            _isInitialized = true;
            
            var packId = (string)query["SelectedPackId"];
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _viewModel.InitializeAsync(packId);
            });
        }
        else if (query.ContainsKey("LoadSaveId"))
        {
            // Set flag immediately to prevent OnAppearing from initializing
            _isInitialized = true;
            
            var saveId = (int)query["LoadSaveId"];
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _viewModel.LoadSavedGameAsync(saveId);
            });
        }
    }
}
