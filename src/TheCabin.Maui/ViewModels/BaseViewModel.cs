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
        var page = GetCurrentPage();
        if (page != null)
        {
            await page.DisplayAlert("Error", message, "OK");
        }
    }

    protected async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var page = GetCurrentPage();
        if (page != null)
        {
            return await page.DisplayAlert(title, message, "Yes", "No");
        }
        return false;
    }

    protected async Task ShowMessageAsync(string title, string message)
    {
        var page = GetCurrentPage();
        if (page != null)
        {
            await page.DisplayAlert(title, message, "OK");
        }
    }

    private Page? GetCurrentPage()
    {
        // Use the recommended way to access the current page in MAUI
        if (Application.Current?.Windows.Count > 0)
        {
            return Application.Current.Windows[0].Page;
        }
        return null;
    }
}
