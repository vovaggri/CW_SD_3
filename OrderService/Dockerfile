# 1) Сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем только csproj и восстанавливаем зависимости
COPY ["OrderService.csproj", "./"]
RUN dotnet restore "./OrderService.csproj"

# Копируем всё остальное и публикуем
COPY . .
RUN dotnet publish "OrderService.csproj" -c Release -o /app

# 2) Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 80
ENTRYPOINT ["dotnet", "OrderService.dll"]
