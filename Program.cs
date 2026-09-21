using System.IO.Compression;
using System.Text;
using System.Text.Json.Serialization;
using Lingua;
using Lingua.Components;
using Lingua.Data;
using Lingua.Hubs;
using Lingua.Models;
using Lingua.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

Configuration.Load(builder.Configuration);

ConfigureDatabase(builder);
ConfigureAuthentication(builder);
ConfigureMvc(builder);
ConfigureServices(builder);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/erro", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseResponseCompression();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await Seeder.SeedAsync(app.Services, app.Configuration);

app.Run();

void ConfigureDatabase(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? "Data Source=lingua.db";

    // SQLite no desenvolvimento para rodar sem dependência externa; PostgreSQL em produção.
    var provider = builder.Configuration["Database:Provider"]
                   ?? (builder.Environment.IsDevelopment() ? "sqlite" : "postgres");

    if (provider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddDbContext<PostgresDataContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<LinguaDataContext>(sp => sp.GetRequiredService<PostgresDataContext>());
    }
    else
    {
        builder.Services.AddDbContext<SqliteDataContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddScoped<LinguaDataContext>(sp => sp.GetRequiredService<SqliteDataContext>());
    }
}

void ConfigureAuthentication(WebApplicationBuilder builder)
{
    var key = Encoding.ASCII.GetBytes(Configuration.JwtKey);

    // O front em Blazor trabalha com cookie; a API REST, com token JWT.
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/entrar";
            options.AccessDeniedPath = "/entrar";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };

            // O hub de chat recebe o token pela query string, que é como o WebSocket consegue mandar.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        context.Token = accessToken;

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(Policies.Teacher, policy => policy.RequireRole(Role.Teacher, Role.Admin))
        .AddPolicy(Policies.Admin, policy => policy.RequireRole(Role.Admin));

    builder.Services.AddCascadingAuthenticationState();
}

void ConfigureMvc(WebApplicationBuilder builder)
{
    builder.Services.AddMemoryCache();

    builder.Services.AddResponseCompression(options =>
    {
        options.Providers.Add<GzipCompressionProvider>();
    });

    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Optimal;
    });

    builder.Services
        .AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        });

    builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    builder.Services.AddSignalR();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddSingleton<ScopeRunner>();
    builder.Services.AddSingleton<ChatNotifier>();

    builder.Services.AddScoped<AccessService>();
    builder.Services.AddScoped<ClassroomService>();
    builder.Services.AddScoped<PostService>();
    builder.Services.AddScoped<ProfileService>();
    builder.Services.AddScoped<ConversationService>();
    builder.Services.AddScoped<InteractionService>();

    builder.Services.AddTransient<TokenService>();
    builder.Services.AddTransient<EmailService>();
}
