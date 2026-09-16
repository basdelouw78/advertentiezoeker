using AdvertentieZoeker.App.Services;
using AdvertentieZoeker.App.Views;
using AdvertentieZoeker.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AdvertentieZoeker.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var dataDirectory = FileSystem.AppDataDirectory;

        builder.Services.AddSingleton(new HttpClient());
        builder.Services.AddSingleton<IMarktplaatsClient, MarktplaatsClient>();
        builder.Services.AddSingleton<ISavedSearchRepository>(_ => new JsonSavedSearchRepository(dataDirectory));
        builder.Services.AddSingleton<ISeenListingRepository>(_ => new JsonSeenListingRepository(Path.Combine(dataDirectory, "gezien")));
        builder.Services.AddSingleton<ISettingsRepository>(_ => new SecureAppSettingsRepository(dataDirectory));
        builder.Services.AddSingleton<FoundListingsLog>(_ => new FoundListingsLog(dataDirectory));

        builder.Services.AddSingleton<INotifier, EmailNotifier>();
        builder.Services.AddSingleton<INotifier, PopupNotifier>();
        builder.Services.AddSingleton<INotifier>(sp => sp.GetRequiredService<FoundListingsLog>());

        builder.Services.AddSingleton<MonitorService>();
        builder.Services.AddSingleton<PollingService>();

        builder.Services.AddTransient<SearchesPage>();
        builder.Services.AddTransient<SearchEditPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<ResultsPage>();

        return builder.Build();
    }
}
