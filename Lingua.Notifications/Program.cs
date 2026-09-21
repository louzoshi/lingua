using Lingua.Notifications;
using Lingua.Notifications.Data;
using Lingua.Notifications.Services;
using Lingua.Notifications.Workers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("SmtpConfiguration"));
builder.Services.Configure<DispatcherOptions>(builder.Configuration.GetSection("Dispatcher"));
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection("Billing"));

ConfigureDatabase(builder);

builder.Services.AddSingleton<EmailSender>();
builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddHostedService<BillingReminderScheduler>();

var host = builder.Build();

host.Run();

void ConfigureDatabase(HostApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? "Data Source=lingua.db";

    // Mesmas chaves do app: os dois processos apontam para o mesmo banco. Aqui não há migration
    // nenhuma — quem cria e versiona as tabelas é o Lingua, que é o dono do schema.
    var provider = builder.Configuration["Database:Provider"] ?? "postgres";

    if (provider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        builder.Services.AddDbContext<NotificationsDataContext>(options => options.UseNpgsql(connectionString));
    else
        builder.Services.AddDbContext<NotificationsDataContext>(options => options.UseSqlite(connectionString));
}
