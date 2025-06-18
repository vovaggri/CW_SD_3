var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ServiceUrls>(
    builder.Configuration.GetSection("Services"));

builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API Gateway", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = string.Empty;  
    c.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "API Gateway v1");
});

app.UseRouting();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();