namespace TheCabin.Maui.Services;

/// <summary>
/// Abstraction for dispatching actions to the main/UI thread.
/// This allows for synchronous execution in unit tests while maintaining async behavior in production.
/// </summary>
public interface IMainThreadDispatcher
{
    /// <summary>
    /// Invokes an action on the main thread.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    void BeginInvokeOnMainThread(Action action);
}
