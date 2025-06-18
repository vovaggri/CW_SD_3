using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderService.Data;
using OrderService.Services;
using OrderService.Settings;
using RabbitMqSettings = OrderService.Settings.RabbitMqSettings;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// 1) EF Core + PostgreSQL
builder.Services.AddDbContext<OrderDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// 3) Считаем настройки RabbitMq (теперь с двумя полями!)
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

// 4) Регистрируем фабрику и канал RabbitMQ
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    return new ConnectionFactory { HostName = cfg.Host };
});
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConnectionFactory>().CreateConnection());
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConnection>().CreateModel());

// 5) Фоновые сервисы: OutboxPublisher и потребитель платежей
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<PaymentConsumer>();

var app = builder.Build();

// 6) Применяем миграции автоматически
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();