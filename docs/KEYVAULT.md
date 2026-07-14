# Mini Commerce — Azure Key Vault

Secrets are **never hardcoded**. At startup each API calls `builder.AddKeyVaultConfiguration()`, which optionally loads Azure Key Vault into `IConfiguration`. Strongly typed options (`JwtOptions`, `ConnectionStringsOptions`, `BlobStorageOptions`, `ServiceBusOptions`, etc.) bind automatically from the combined configuration sources.

---

## Behaviour by environment

| Environment | `KeyVault:Enabled` | Auth | Secret source |
|-------------|--------------------|------|----------------|
| **Local Development** | `false` (default) | — | `appsettings.json` + **User Secrets** + env vars |
| **Local + Key Vault** | `true` | Shared `DefaultAzureCredential` (`AzureAuth`) | Key Vault overlays local values |
| **Production / AKS** | `true` | **Managed Identity** via `DefaultAzureCredential` | Key Vault only for secrets (appsettings leave secrets empty) |

Full credential provider guide: [`docs/AZURE-AUTH.md`](AZURE-AUTH.md).

Configuration precedence (highest wins after Key Vault is added):

1. Azure Key Vault (when enabled)
2. Environment variables
3. User Secrets (Development)
4. `appsettings.{Environment}.json`
5. `appsettings.json`

---

## Registration (every `Program.cs`)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMiniCommerceAzureCredential(builder.Configuration);
builder.AddKeyVaultConfiguration(); // BuildingBlocks — automatic secret loading
builder.Services.AddMiniCommerceOptions(builder.Configuration); // IOptions<T> bind after Key Vault
```

Implementation: `MiniCommerce.BuildingBlocks.Configuration.KeyVaultConfigurationExtensions`.

When `Enabled=false`, the call is a **no-op** — local development keeps working unchanged.

---

## Configuration (`KeyVault` section)

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `Enabled` | `false` | `KeyVault__Enabled` | Load secrets from Key Vault |
| `VaultUri` | — | `KeyVault__VaultUri` | `https://{name}.vault.azure.net/` |
| `ReloadIntervalMinutes` | `30` | `KeyVault__ReloadIntervalMinutes` | Periodic secret reload (`0` = off) |

Auth is controlled by **`AzureAuth`** (`PreferManagedIdentity`, `ManagedIdentityClientId`), not Key Vault-specific credentials. Legacy `KeyVault:UseManagedIdentity` / `ManagedIdentityClientId` are obsolete.

---

## Secret naming (`--` → `:`)

Key Vault secret names cannot contain `:`. The ASP.NET Core Key Vault provider maps `--` to nested configuration keys.

| Key Vault secret name | Configuration key | Purpose |
|-----------------------|-------------------|---------|
| `Jwt--SigningKey` | `Jwt:SigningKey` | JWT secret |
| `Jwt--Issuer` | `Jwt:Issuer` | JWT issuer |
| `Jwt--Audience` | `Jwt:Audience` | JWT audience |
| `ConnectionStrings--OrderDB` | `ConnectionStrings:OrderDB` | SQL |
| `ConnectionStrings--InventoryDB` | `ConnectionStrings:InventoryDB` | SQL |
| `ConnectionStrings--NotificationDB` | `ConnectionStrings:NotificationDB` | SQL |
| `ConnectionStrings--AuthDB` | `ConnectionStrings:AuthDB` | SQL |
| `ConnectionStrings--CatalogDB` | `ConnectionStrings:CatalogDB` | SQL |
| `ConnectionStrings--CartDB` | `ConnectionStrings:CartDB` | SQL |
| `ServiceBus--ConnectionString` | `ServiceBus:ConnectionString` | Service Bus (local/string auth) |
| `ServiceBus--FullyQualifiedNamespace` | `ServiceBus:FullyQualifiedNamespace` | Service Bus (MI) |
| `BlobStorage--ConnectionString` | `BlobStorage:ConnectionString` | Blob (local) |
| `BlobStorage--ServiceUri` | `BlobStorage:ServiceUri` | Blob (MI) |
| `BlobStorage--AccountName` | `BlobStorage:AccountName` | Blob account |
| `ApplicationInsights--ConnectionString` | `ApplicationInsights:ConnectionString` | Telemetry / API instrumentation key material |
| `ApiKeys--Internal` | `ApiKeys:Internal` | Optional internal API key |

Canonical constants: `KeyVaultSecretNames` in BuildingBlocks.

---

## Local development (no Key Vault)

1. Keep `KeyVault:Enabled=false` in `appsettings.json`.
2. Use User Secrets (each API already has `UserSecretsId`):

```bash
dotnet user-secrets set "Jwt:SigningKey" "your-local-dev-key-32chars-min" --project src/AuthService.API
dotnet user-secrets set "ConnectionStrings:OrderDB" "Server=localhost,1433;..." --project src/OrderService.API
```

3. Or rely on non-secret defaults already in `appsettings.json` for SQL/JWT **dev-only** values.

## Local development (with Key Vault)

```bash
az login
# appsettings.Development.json or env:
# KeyVault__Enabled=true
# KeyVault__VaultUri=https://your-vault.vault.azure.net/
# KeyVault__UseManagedIdentity=false
```

`DefaultAzureCredential` will use your Azure CLI identity (must have Key Vault Secrets User).

## Production

```bash
KeyVault__Enabled=true
KeyVault__VaultUri=https://kv-minimart.vault.azure.net/
# UseManagedIdentity defaults to true when ASPNETCORE_ENVIRONMENT=Production
# Optional user-assigned / workload identity:
KeyVault__ManagedIdentityClientId=<client-id>
```

Leave connection strings / JWT signing key **empty** in `appsettings.Production.json` so values come only from Key Vault (or CSI-injected env).

---

## Provision placeholders

```powershell
.\scripts\provision-keyvault.ps1 -ResourceGroup rg-minimart-prod -KeyVaultName kv-minimart -Location eastus
```

Creates Key Vault and placeholder secrets for JWT, connection strings, Service Bus, Blob, Application Insights, and API keys. Replace `REPLACE_ME` with real values before go-live.

AKS CSI alternative: `deploy/kubernetes/secret-provider-class.yaml` (injects secrets as env vars). Apps can use **either** CSI **or** SDK Key Vault loading.

---

## Files involved

| File | Role |
|------|------|
| `KeyVaultOptions.cs` | Options + `KeyVaultSecretNames` |
| `KeyVaultConfigurationExtensions.cs` | `AddKeyVaultConfiguration()` |
| `KeyVaultHealthCheck.cs` | `/health/ready` check when enabled |
| Every API `Program.cs` | Calls `builder.AddKeyVaultConfiguration()` early |

**Business logic, controllers, repositories, and APIs are unchanged.**
