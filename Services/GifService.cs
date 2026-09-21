using System.Text.Json;

namespace Lingua.Services;

/// <summary>
/// Busca de GIFs no Tenor. Precisa de <c>Tenor:ApiKey</c> na configuração; sem a chave, o
/// seletor de GIF explica isso e aceita só a URL de um GIF colada à mão.
/// </summary>
public class GifService
{
    private readonly HttpClient _http;

    public GifService(HttpClient http)
        => _http = http;

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(Configuration.TenorApiKey);

    public async Task<List<GifResult>> SearchAsync(string query, int limit = 24)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(query))
            return new List<GifResult>();

        var url = "https://tenor.googleapis.com/v2/search" +
                  $"?q={Uri.EscapeDataString(query)}" +
                  $"&key={Uri.EscapeDataString(Configuration.TenorApiKey)}" +
                  $"&limit={limit}&media_filter=gif,tinygif&contentfilter=medium";

        using var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return new List<GifResult>();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        var results = new List<GifResult>();

        foreach (var item in document.RootElement.GetProperty("results").EnumerateArray())
        {
            if (!item.TryGetProperty("media_formats", out var formats))
                continue;

            var full = formats.TryGetProperty("gif", out var gif) ? gif.GetProperty("url").GetString() : null;
            var tiny = formats.TryGetProperty("tinygif", out var tinyGif) ? tinyGif.GetProperty("url").GetString() : full;

            if (full != null)
                results.Add(new GifResult(full, tiny ?? full));
        }

        return results;
    }
}

public record GifResult(string Url, string PreviewUrl);
