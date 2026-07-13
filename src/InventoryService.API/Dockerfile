# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["MiniCommerce.sln", "./"]
COPY ["src/MiniCommerce.Contracts/MiniCommerce.Contracts.csproj", "src/MiniCommerce.Contracts/"]
COPY ["src/MiniCommerce.BuildingBlocks/MiniCommerce.BuildingBlocks.csproj", "src/MiniCommerce.BuildingBlocks/"]
COPY ["src/InventoryService.Domain/InventoryService.Domain.csproj", "src/InventoryService.Domain/"]
COPY ["src/InventoryService.Application/InventoryService.Application.csproj", "src/InventoryService.Application/"]
COPY ["src/InventoryService.Infrastructure/InventoryService.Infrastructure.csproj", "src/InventoryService.Infrastructure/"]
COPY ["src/InventoryService.API/InventoryService.API.csproj", "src/InventoryService.API/"]
RUN dotnet restore "src/InventoryService.API/InventoryService.API.csproj"
COPY . .
WORKDIR /src/src/InventoryService.API
RUN dotnet publish "InventoryService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/* \
    && adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
EXPOSE 8081
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:8081/api/health/live || exit 1
ENTRYPOINT ["dotnet", "InventoryService.API.dll"]
