using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using PaymentsService.Data;
using PaymentsService.Settings;
using PaymentsService.Services;

var builder = WebApplication.CreateBuilder(args);

// 1) EF Core
builder.Services.AddDbContext<PaymentDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3) Настройки RabbitMQ
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

// 4) DI RabbitMQ: фабрика, соединение и канал
builder.Services.AddSingleton<IConnectionFactory>(sp => {
    var s = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    return new ConnectionFactory { HostName = s.Host };
});
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConnectionFactory>().CreateConnection());
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConnection>().CreateModel());

// 5) Фоновые сервисы
builder.Services.AddHostedService<PaymentConsumer>();
builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();