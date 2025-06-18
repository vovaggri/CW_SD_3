using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using PaymentsService.Data;
using PaymentsService.Services;
using PaymentsService.Settings;

var builder = WebApplication.CreateBuilder(args);

// 1) Подключаем EF Core + PostgreSQL
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Добавляем контроллеры и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3) Конфигурируем настройки RabbitMQ
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

// 5) Регистрируем фоновые сервисы: PaymentConsumer и OutboxPublisher
builder.Services.AddHostedService<PaymentConsumer>();
builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();

// 6) Гарантированно применяем миграции при старте
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

// 7) Swagger
app.UseSwagger();
app.UseSwaggerUI();

// 8) Маршрутизация контроллеров
app.MapControllers();

app.Run();