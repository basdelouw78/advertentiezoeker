using AdvertentieZoeker.Core.Services;

namespace AdvertentieZoeker.App.Views;

public partial class SettingsPage : ContentPage
{
    private readonly ISettingsRepository _settingsRepository;

    public SettingsPage(ISettingsRepository settingsRepository)
    {
        InitializeComponent();
        _settingsRepository = settingsRepository;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var settings = await _settingsRepository.GetAsync();

        PollIntervalEntry.Text = settings.PollIntervalMinutes.ToString();
        PopupSwitch.IsToggled = settings.PopupNotificationsEnabled;

        EmailEnabledSwitch.IsToggled = settings.Email.Enabled;
        SmtpHostEntry.Text = settings.Email.SmtpHost;
        SmtpPortEntry.Text = settings.Email.SmtpPort.ToString();
        SmtpSslSwitch.IsToggled = settings.Email.UseSsl;
        SmtpUsernameEntry.Text = settings.Email.Username;
        SmtpPasswordEntry.Text = settings.Email.Password;
        FromAddressEntry.Text = settings.Email.FromAddress;
        ToAddressEntry.Text = settings.Email.ToAddress;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (!int.TryParse(PollIntervalEntry.Text, out var pollInterval) || pollInterval < 5)
        {
            await DisplayAlert("Ongeldig interval", "Vul een getal van minimaal 5 minuten in.", "Ok");
            return;
        }

        if (!int.TryParse(SmtpPortEntry.Text, out var smtpPort))
        {
            smtpPort = 587;
        }

        var settings = await _settingsRepository.GetAsync();
        settings.PollIntervalMinutes = pollInterval;
        settings.PopupNotificationsEnabled = PopupSwitch.IsToggled;
        settings.Email.Enabled = EmailEnabledSwitch.IsToggled;
        settings.Email.SmtpHost = SmtpHostEntry.Text?.Trim() ?? string.Empty;
        settings.Email.SmtpPort = smtpPort;
        settings.Email.UseSsl = SmtpSslSwitch.IsToggled;
        settings.Email.Username = SmtpUsernameEntry.Text?.Trim() ?? string.Empty;
        settings.Email.Password = SmtpPasswordEntry.Text ?? string.Empty;
        settings.Email.FromAddress = FromAddressEntry.Text?.Trim() ?? string.Empty;
        settings.Email.ToAddress = ToAddressEntry.Text?.Trim() ?? string.Empty;

        await _settingsRepository.SaveAsync(settings);
        await DisplayAlert("Opgeslagen", "Instellingen zijn opgeslagen.", "Ok");
    }
}
