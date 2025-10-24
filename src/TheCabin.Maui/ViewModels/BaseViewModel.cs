using CommunityToolkit.Mvvm.ComponentModel;

namespace TheCabin.Maui.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    protected async Task ExecuteAsync(Func<Task> operation, string? errorMessage = null)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            await operation();
        }
        catch (Exception ex)
        {
            ErrorMessage = errorMessage ?? ex.Message;
            await ShowErrorAsync(ErrorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task ShowErrorAsync(string message)
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert("Error", message, "OK");
        }
    }

    protected async Task<bool> ShowConfirmAsync(string title, string message)
    {
        if (Application.Current?.MainPage != null)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
        }
        return false;
    }

    protected async Task ShowMessageAsync(string title, string message)
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }
    }
}
