namespace TheCabin.Maui;

public partial class App : Application
{
    public App(AppShell appShell)
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Use the recommended CreateWindow approach instead of MainPage
        return new Window(Handler.MauiContext!.Services.GetRequiredService<AppShell>());
    }

    protected override void OnStart()
    {
        base.OnStart();
        
        // Initialize story packs asynchronously after app starts
        // This avoids blocking the UI thread during startup
        Task.Run(async () => await InitializeStoryPacksAsync());
    }

    private async Task InitializeStoryPacksAsync()
    {
        try
        {
            var storyPackPath = Path.Combine(FileSystem.AppDataDirectory, "story_packs");
            
            // Story pack files to copy from Resources/Raw
            var storyPacks = new[]
            {
                "classic_horror.json",
                "arctic_survival.json",
                "fantasy_magic.json",
                "sci_fi_isolation.json",
                "cozy_mystery.json"
            };
            
            foreach (var packFile in storyPacks)
            {
                var targetFile = Path.Combine(storyPackPath, packFile);
                
                // Only copy if doesn't exist (don't overwrite user modifications)
                if (!File.Exists(targetFile))
                {
                    try
                    {
                        using var stream = await FileSystem.OpenAppPackageFileAsync(packFile);
                        using var reader = new StreamReader(stream);
                        var content = await reader.ReadToEndAsync();
                        await File.WriteAllTextAsync(targetFile, content);
                        
                        System.Diagnostics.Debug.WriteLine($"Successfully copied {packFile}");
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail - app can still work without story packs
                        System.Diagnostics.Debug.WriteLine($"Failed to copy {packFile}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Story pack initialization failed: {ex.Message}");
        }
    }
}
