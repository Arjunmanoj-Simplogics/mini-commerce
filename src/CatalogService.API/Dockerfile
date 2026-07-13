FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["MiniCommerce.sln", "./"]
COPY ["src/MiniCommerce.BuildingBlocks/MiniCommerce.BuildingBlocks.csproj", "src/MiniCommerce.BuildingBlocks/"]
COPY ["src/CatalogService.API/CatalogService.API.csproj", "src/CatalogService.API/"]
RUN dotnet restore "src/CatalogService.API/CatalogService.API.csproj"
COPY . .
WORKDIR /src/src/CatalogService.API
RUN dotnet publish "CatalogService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8084
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/* \
    && adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
EXPOSE 8084
HEALTHCHECK CMD curl -f http://localhost:8084/api/health/live || exit 1
ENTRYPOINT ["dotnet", "CatalogService.API.dll"]
