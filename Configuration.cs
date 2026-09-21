namespace Lingua;

public static class Configuration
{
    public static string JwtKey { get; private set; } = string.Empty;

    /// <summary>Endereço público da aplicação, usado para montar URLs de arquivos enviados.</summary>
    public static string PublicUrl { get; private set; } = "https://localhost:5001";

    /// <summary>Chave do Tenor para a busca de GIFs. Vazia desliga a busca.</summary>
    public static string TenorApiKey { get; private set; } = string.Empty;

    public static void Load(IConfiguration configuration)
    {
        JwtKey = configuration["JwtKey"] ?? string.Empty;
        PublicUrl = (configuration["PublicUrl"] ?? PublicUrl).TrimEnd('/');
        TenorApiKey = configuration["Tenor:ApiKey"] ?? string.Empty;
    }
}
