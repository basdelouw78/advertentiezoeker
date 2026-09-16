using AdvertentieZoeker.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AdvertentieZoeker.Core.Services;

public sealed class EmailNotifier : INotifier
{
    private readonly ISettingsRepository _settingsRepository;

    public EmailNotifier(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task NotifyNewListingsAsync(SavedSearch search, IReadOnlyList<Listing> newListings, CancellationToken cancellationToken = default)
    {
        if (newListings.Count == 0)
        {
            return;
        }

        var settings = await _settingsRepository.GetAsync(cancellationToken).ConfigureAwait(false);
        var email = settings.Email;
        if (!email.Enabled
            || string.IsNullOrWhiteSpace(email.SmtpHost)
            || string.IsNullOrWhiteSpace(email.ToAddress)
            || string.IsNullOrWhiteSpace(email.FromAddress))
        {
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(email.FromAddress));
        message.To.Add(MailboxAddress.Parse(email.ToAddress));
        message.Subject = newListings.Count == 1
            ? $"Advertentiezoeker: nieuwe advertentie voor \"{search.Name}\""
            : $"Advertentiezoeker: {newListings.Count} nieuwe advertenties voor \"{search.Name}\"";

        var body = new BodyBuilder { HtmlBody = BuildHtmlBody(search, newListings) };
        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = email.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(email.SmtpHost, email.SmtpPort, socketOptions, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(email.Username))
        {
            await client.AuthenticateAsync(email.Username, email.Password, cancellationToken).ConfigureAwait(false);
        }

        await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildHtmlBody(SavedSearch search, IReadOnlyList<Listing> newListings)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"<p>Nieuwe advertenties gevonden voor zoekopdracht <b>{System.Net.WebUtility.HtmlEncode(search.Name)}</b>:</p><ul>");
        foreach (var listing in newListings)
        {
            var afstand = listing.Location.DistanceKm is { } km ? $" &middot; {km:0.#} km" : string.Empty;
            var plaats = listing.Location.City is { } city ? $" &middot; {System.Net.WebUtility.HtmlEncode(city)}" : string.Empty;
            sb.Append("<li>");
            sb.Append($"<a href=\"{listing.Url}\">{System.Net.WebUtility.HtmlEncode(listing.Title)}</a>");
            sb.Append($" &mdash; €{listing.PriceInEuros:0.00}{plaats}{afstand}");
            sb.Append("</li>");
        }
        sb.Append("</ul>");
        return sb.ToString();
    }
}
