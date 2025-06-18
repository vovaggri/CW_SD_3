# 1) Сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PaymentsService.csproj", "./"]
RUN dotnet restore "./PaymentsService.csproj"

COPY . .
RUN dotnet publish "PaymentsService.csproj" -c Release -o /app

# 2) Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 80
ENTRYPOINT ["dotnet", "PaymentsService.dll"]
