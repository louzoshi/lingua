namespace Lingua;

public static class Configuration
{
    public static string JwtKey { get; private set; } = string.Empty;

    /// <summary>Endereço público da aplicação, usado para montar URLs de arquivos enviados.</summary>
    public static string PublicUrl { get; private set; } = "https://localhost:5001";

    public static SmtpConfiguration Smtp { get; private set; } = new();

    public static void Load(IConfiguration configuration)
    {
        JwtKey = configuration["JwtKey"] ?? string.Empty;
        PublicUrl = (configuration["PublicUrl"] ?? PublicUrl).TrimEnd('/');

        var smtp = new SmtpConfiguration();
        configuration.GetSection("SmtpConfiguration").Bind(smtp);
        Smtp = smtp;
    }

    public class SmtpConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromName { get; set; } = "Lingua";
        public string FromEmail { get; set; } = "no-reply@lingua.local";
    }
}
