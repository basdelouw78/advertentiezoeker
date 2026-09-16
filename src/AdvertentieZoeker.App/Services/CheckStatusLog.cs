using System.Text.Json;

namespace AdvertentieZoeker.App.Services;

/// <summary>
/// Bewaart het resultaat van de laatste controle-cyclus, zodat je in de app kunt zien
/// wanneer er voor het laatst gecontroleerd is en of dat is gelukt — zonder dat je
/// daarvoor een debugger of adb logcat nodig hebt.
/// </summary>
public sealed class CheckStatusLog
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CheckStatusLog(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "laatste-controle.json");
    }

    public async Task RecordSuccessAsync(IReadOnlyList<SearchCheckStatus> perSearch, CancellationToken cancellationToken = default)
    {
        var totalNew = perSearch.Sum(s => s.NewListingsCount);
        await WriteAsync(new CheckStatus(DateTimeOffset.Now, true, totalNew, null, perSearch), cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordFailureAsync(string errorMessage, CancellationToken cancellationToken = default)
    {
        await WriteAsync(new CheckStatus(DateTimeOffset.Now, false, 0, errorMessage, Array.Empty<SearchCheckStatus>()), cancellationToken).ConfigureAwait(false);
    }

    public async Task<CheckStatus?> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<CheckStatus>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task WriteAsync(CheckStatus status, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(status);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }
}

public sealed record CheckStatus(
    DateTimeOffset CheckedAt,
    bool Success,
    int NewListingsCount,
    string? ErrorMessage,
    IReadOnlyList<SearchCheckStatus>? PerSearch);

/// <summary>Resultaat van één individuele zoekopdracht binnen de laatste controle-ronde.</summary>
public sealed record SearchCheckStatus(Guid SearchId, string SearchName, int NewListingsCount, string? ErrorMessage);
