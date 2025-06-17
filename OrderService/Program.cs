using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Services;
using RabbitMqSettings = OrderService.Settings.RabbitMqSettings;

var builder = WebApplication.CreateBuilder(args);
// EF Core
builder.Services.AddDbContext<OrderDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// RabbitMQ settings
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
// Hosted Outbox Publisher
//builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();