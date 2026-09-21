using Lingua.Models;

namespace Lingua.Services;

/// <summary>
/// Guarda os arquivos que os usuários enviam. Mídia de post vai para <c>wwwroot/uploads</c>
/// e é servida como estático, com nome aleatório. Contratos vão para <c>App_Data/contracts</c>,
/// fora do <c>wwwroot</c>, e só saem por um endpoint restrito à professora.
/// </summary>
public class MediaStorage
{
    public const long MaxImageBytes = 8 * 1024 * 1024;
    public const long MaxVideoBytes = 120 * 1024 * 1024;
    public const long MaxContractBytes = 20 * 1024 * 1024;

    private static readonly Dictionary<string, (string Extension, MediaKind Kind)> AllowedMedia = new()
    {
        ["image/jpeg"] = (".jpg", MediaKind.Image),
        ["image/png"] = (".png", MediaKind.Image),
        ["image/webp"] = (".webp", MediaKind.Image),
        ["image/gif"] = (".gif", MediaKind.Gif),
        ["video/mp4"] = (".mp4", MediaKind.Video),
        ["video/webm"] = (".webm", MediaKind.Video),
        ["video/quicktime"] = (".mov", MediaKind.Video)
    };

    private readonly IWebHostEnvironment _environment;

    public MediaStorage(IWebHostEnvironment environment)
        => _environment = environment;

    public string ContractsFolder => Path.Combine(_environment.ContentRootPath, "App_Data", "contracts");

    private string UploadsFolder => Path.Combine(_environment.WebRootPath, "uploads");

    public static bool IsAllowedMedia(string contentType)
        => AllowedMedia.ContainsKey(contentType);

    public static MediaKind KindOf(string contentType)
        => AllowedMedia.TryGetValue(contentType, out var entry) ? entry.Kind : MediaKind.Image;

    public static long LimitFor(string contentType)
        => KindOf(contentType) == MediaKind.Video ? MaxVideoBytes : MaxImageBytes;

    /// <summary>Salva a mídia e devolve a URL pública dela.</summary>
    public async Task<(string? Url, MediaKind Kind, string? Error)> SaveMediaAsync(Stream content, string contentType)
    {
        if (!AllowedMedia.TryGetValue(contentType, out var entry))
            return (null, MediaKind.Image, "Formato não aceito. Envie JPG, PNG, WEBP, GIF, MP4 ou WEBM.");

        Directory.CreateDirectory(UploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{entry.Extension}";

        await using (var file = File.Create(Path.Combine(UploadsFolder, fileName)))
            await content.CopyToAsync(file);

        return ($"/uploads/{fileName}", entry.Kind, null);
    }

    /// <summary>Salva um contrato em PDF fora do wwwroot e devolve o nome interno do arquivo.</summary>
    public async Task<(string? FileName, string? Error)> SaveContractAsync(Stream content, string contentType)
    {
        if (contentType != "application/pdf")
            return (null, "O contrato precisa ser um PDF.");

        Directory.CreateDirectory(ContractsFolder);

        var fileName = $"{Guid.NewGuid():N}.pdf";

        await using (var file = File.Create(Path.Combine(ContractsFolder, fileName)))
            await content.CopyToAsync(file);

        return (fileName, null);
    }

    public string? ContractPath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.Contains('/'))
            return null;

        var path = Path.Combine(ContractsFolder, fileName);
        return File.Exists(path) ? path : null;
    }

    public void DeleteContract(string? fileName)
    {
        var path = ContractPath(fileName);
        if (path != null)
            File.Delete(path);
    }
}
