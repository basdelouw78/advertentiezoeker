using AdvertentieZoeker.App.Views;

namespace AdvertentieZoeker.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(SearchEditPage), typeof(SearchEditPage));
    }
}
