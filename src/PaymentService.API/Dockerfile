FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["MiniCommerce.sln", "./"]
COPY ["src/MiniCommerce.BuildingBlocks/MiniCommerce.BuildingBlocks.csproj", "src/MiniCommerce.BuildingBlocks/"]
COPY ["src/PaymentService.API/PaymentService.API.csproj", "src/PaymentService.API/"]
RUN dotnet restore "src/PaymentService.API/PaymentService.API.csproj"
COPY . .
WORKDIR /src/src/PaymentService.API
RUN dotnet publish "PaymentService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8086
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/* \
    && adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
EXPOSE 8086
HEALTHCHECK CMD curl -f http://localhost:8086/api/health/live || exit 1
ENTRYPOINT ["dotnet", "PaymentService.API.dll"]
