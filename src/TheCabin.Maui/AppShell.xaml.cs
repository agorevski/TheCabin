using TheCabin.Maui.Views;

namespace TheCabin.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register MainPage route for navigation
        Routing.RegisterRoute("MainPage", typeof(MainPage));
    }
}
