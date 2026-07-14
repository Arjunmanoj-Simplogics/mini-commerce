# syntax=docker/dockerfile:1.7
# Cart Service — production multi-stage image.
# Context: repository root
#   docker build -f deploy/docker/CartService.Dockerfile -t minicommerce/cart-service:latest .

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src

COPY Directory.Build.props global.json MiniCommerce.sln ./
COPY src/MiniCommerce.AzureAuth/MiniCommerce.AzureAuth.csproj src/MiniCommerce.AzureAuth/
COPY src/MiniCommerce.Contracts/MiniCommerce.Contracts.csproj src/MiniCommerce.Contracts/
COPY src/MiniCommerce.Storage/MiniCommerce.Storage.csproj src/MiniCommerce.Storage/
COPY src/MiniCommerce.Messaging/MiniCommerce.Messaging.csproj src/MiniCommerce.Messaging/
COPY src/MiniCommerce.BuildingBlocks/MiniCommerce.BuildingBlocks.csproj src/MiniCommerce.BuildingBlocks/
COPY src/CartService.API/CartService.API.csproj src/CartService.API/

RUN dotnet restore src/CartService.API/CartService.API.csproj

FROM restore AS publish
COPY . .
RUN dotnet publish src/CartService.API/CartService.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0 \
    TZ=UTC

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --chown=app:app --from=publish /app/publish .
USER app

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=25s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "CartService.API.dll"]
