<#
.SYNOPSIS
  Tests SQL connectivity using Microsoft.Data.SqlClient (same stack as the APIs).

.DESCRIPTION
  Pass connection strings via environment variables (ConnectionStrings__*).
  If none are set, defaults to local Docker Compose SA credentials on localhost:1433.

.EXAMPLE
  .\scripts\Test-SqlConnectivity.ps1

.EXAMPLE
  $env:ConnectionStrings__OrderDB = "Server=tcp:myserver.database.windows.net,1433;Database=OrderDB;..."
  .\scripts\Test-SqlConnectivity.ps1
#>
[CmdletBinding()]
param(
    [string]$Server = "localhost,1433",
    [string]$User = "sa",
    [string]$Password = "Your_strong_Password123"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$databases = @(
    @{ Name = "OrderDB"; Env = "ConnectionStrings__OrderDB" },
    @{ Name = "InventoryDB"; Env = "ConnectionStrings__InventoryDB" },
    @{ Name = "NotificationDB"; Env = "ConnectionStrings__NotificationDB" },
    @{ Name = "AuthDB"; Env = "ConnectionStrings__AuthDB" },
    @{ Name = "CatalogDB"; Env = "ConnectionStrings__CatalogDB" },
    @{ Name = "CartDB"; Env = "ConnectionStrings__CartDB" }
)

$probeDir = Join-Path $env:TEMP "minicommerce-sql-probe"
New-Item -ItemType Directory -Force -Path $probeDir | Out-Null
$csproj = Join-Path $probeDir "SqlProbe.csproj"
$program = Join-Path $probeDir "Program.cs"

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.2" />
  </ItemGroup>
</Project>
"@ | Set-Content -Path $csproj -Encoding UTF8

@"
using Microsoft.Data.SqlClient;

var name = args[0];
var cs = args[1];
Console.WriteLine(`$"Probing {name}...");
await using var conn = new SqlConnection(cs);
await conn.OpenAsync();
await using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT 1";
var result = await cmd.ExecuteScalarAsync();
Console.WriteLine(`$"OK {name}: SELECT 1 = {result}; ServerVersion = {conn.ServerVersion}");
"@ | Set-Content -Path $program -Encoding UTF8

Write-Host "Restoring SQL probe tool..." -ForegroundColor Cyan
dotnet restore $csproj | Out-Null
dotnet build $csproj -c Release --no-restore | Out-Null

$failed = 0
foreach ($db in $databases) {
    $cs = [Environment]::GetEnvironmentVariable($db.Env)
    if ([string]::IsNullOrWhiteSpace($cs)) {
        $cs = "Server=$Server;Database=$($db.Name);User Id=$User;Password=$Password;TrustServerCertificate=True;Encrypt=False"
    }

    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor DarkGray
    try {
        & dotnet run --project $csproj -c Release --no-build -- $($db.Name) $cs
        if ($LASTEXITCODE -ne 0) { throw "Probe exited with $LASTEXITCODE" }
    }
    catch {
        Write-Host "FAILED $($db.Name): $_" -ForegroundColor Red
        $failed++
    }
}

Write-Host ""
if ($failed -eq 0) {
    Write-Host "All SQL connectivity probes succeeded." -ForegroundColor Green
    exit 0
}

Write-Host "$failed database probe(s) failed. Ensure SQL is running (e.g. docker compose up -d sqlserver)." -ForegroundColor Red
exit 1
