namespace TheCabin.Maui.Services;

/// <summary>
/// Production implementation that dispatches to the actual MAUI main thread.
/// </summary>
public class MauiMainThreadDispatcher : IMainThreadDispatcher
{
    public void BeginInvokeOnMainThread(Action action)
    {
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(action);
    }
}
