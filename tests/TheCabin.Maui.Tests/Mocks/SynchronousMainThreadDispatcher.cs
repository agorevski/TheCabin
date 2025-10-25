using TheCabin.Maui.Services;

namespace TheCabin.Maui.Tests.Mocks;

/// <summary>
/// Test implementation that executes actions synchronously on the current thread.
/// This avoids the NotImplementedInReferenceAssemblyException when testing.
/// </summary>
public class SynchronousMainThreadDispatcher : IMainThreadDispatcher
{
    public void BeginInvokeOnMainThread(Action action)
    {
        // Execute synchronously for unit tests
        action();
    }
}
