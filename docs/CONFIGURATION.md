# Mini Commerce — Configuration Reference

All services load configuration through ASP.NET Core `IConfiguration` with strongly typed `IOptions<T>` bindings in `MiniCommerce.BuildingBlocks`.

**Precedence (highest wins):** environment variables → Key Vault (when enabled) → `appsettings.{Environment}.json` → `appsettings.json`.

**Environment variable syntax:** nested keys use `__`, arrays use `__0`, `__1`, etc.  
Example: `ConnectionStrings__OrderDB`, `Cors__AllowedOrigins__0`, `Jwt__SigningKey`.

---

## Registration

Every API calls during startup:

```csharp
builder.Services.AddMiniCommerceAzureCredential(builder.Configuration); // DefaultAzureCredential
builder.AddKeyVaultConfiguration();           // optional Key Vault source
builder.Services.AddMiniCommerceOptions(builder.Configuration);  // IOptions<T> for all sections
builder.Services.AddMiniCommerceTelemetry(builder.Configuration); // OpenTelemetry traces/metrics/logs
builder.Services.AddMiniCommerceCors(builder.Configuration);     // FrontendPolicy
builder.Services.AddMiniCommerceJwtAuthentication(builder.Configuration); // JWT bearer (where required)
```

Shared Azure auth (Dev connection strings / Prod Managed Identity): [`AZURE-AUTH.md`](AZURE-AUTH.md).  
Observability (OpenTelemetry): [`OBSERVABILITY.md`](OBSERVABILITY.md).  
Kubernetes application readiness (no manifests): [`KUBERNETES-READY.md`](KUBERNETES-READY.md).

Connection strings are resolved via `IConfiguration.GetRequiredSqlConnectionString(name)` or `IOptions<ConnectionStringsOptions>`.

---

## Connection strings (`ConnectionStrings` / `ConnectionStringsOptions`)

| Key | Service | Env variable |
|-----|---------|--------------|
| `OrderDB` | Order | `ConnectionStrings__OrderDB` |
| `InventoryDB` | Inventory | `ConnectionStrings__InventoryDB` |
| `NotificationDB` | Notification | `ConnectionStrings__NotificationDB` |
| `AuthDB` | Auth | `ConnectionStrings__AuthDB` |
| `CatalogDB` | Catalog | `ConnectionStrings__CatalogDB` |
| `CartDB` | Cart | `ConnectionStrings__CartDB` |

**Local:** set in each service `appsettings.json`.  
**Production:** leave empty in `appsettings.Production.json`; supply via env, Key Vault (`ConnectionStrings--OrderDB`), or Kubernetes CSI.

---

## SqlOptions (`Database` section)

EF Core behavior for SQL Server (retries, timeouts, startup migration).

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `CommandTimeoutSeconds` | `30` | `Database__CommandTimeoutSeconds` | EF command timeout |
| `MaxRetryCount` | `5` | `Database__MaxRetryCount` | `EnableRetryOnFailure` count |
| `MaxRetryDelaySeconds` | `30` | `Database__MaxRetryDelaySeconds` | Max delay between retries |
| `AutoMigrate` | `true` | `Database__AutoMigrate` | Run migrate/ensure-created on startup |
| `UseManagedIdentity` | follows `AzureAuth` | `Database__UseManagedIdentity` | Azure SQL AAD Default (Managed Identity) |

**Options class:** `MiniCommerce.BuildingBlocks.Configuration.SqlOptions`  
**Legacy alias:** `AzureSqlOptions` (obsolete, inherits `SqlOptions`).

**Local:** SQL user/password in the connection string.  
**Production:** Managed Identity via `Authentication=Active Directory Default` — [`AZURE-AUTH.md`](AZURE-AUTH.md).

---

## JwtOptions (`Jwt` section)

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `Issuer` | `MiniCommerce` | `Jwt__Issuer` | JWT issuer claim |
| `Audience` | `MiniCommerce` | `Jwt__Audience` | JWT audience claim |
| `SigningKey` | *(dev placeholder)* | `Jwt__SigningKey` | Symmetric signing key (≥32 chars) |
| `ExpirationMinutes` | `120` | `Jwt__ExpirationMinutes` | Access token lifetime |

**Used by:** Auth (issuance), Order, Inventory, Catalog, Cart, Payment (validation).  
**Not used by:** Notification (no JWT).

---

## CorsOptions (`Cors` section)

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `AllowedOrigins` | `["http://localhost:5173"]` | `Cors__AllowedOrigins__0`, … | Browser origins for the SPA |

Policy name: `FrontendPolicy` (`CorsOptions.FrontendPolicyName`).

---

## BlobStorageOptions (`BlobStorage` section)

Defined in **`MiniCommerce.Storage`**. Full guide: [`STORAGE.md`](STORAGE.md).

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `Enabled` | `false` | `BlobStorage__Enabled` | Register blob service + health check |
| `ServiceUri` | — | `BlobStorage__ServiceUri` | Azure blob endpoint (MI auth) |
| `AccountName` | — | `BlobStorage__AccountName` | Alternative to ServiceUri |
| `ContainerName` | `product-images` | `BlobStorage__ContainerName` | Upload container |
| `ConnectionString` | — | `BlobStorage__ConnectionString` | **Local only** — do not use in Azure |
| `MaxRetryCount` | `5` | `BlobStorage__MaxRetryCount` | SDK exponential retries |
| `RetryDelaySeconds` | `1` | `BlobStorage__RetryDelaySeconds` | Initial retry delay |
| `MaxRetryDelaySeconds` | `30` | `BlobStorage__MaxRetryDelaySeconds` | Max retry backoff |
| `NetworkTimeoutSeconds` | `60` | `BlobStorage__NetworkTimeoutSeconds` | Per-attempt network timeout |

**Used by:** Catalog (product image upload). Only blob **URLs** are stored in SQL.

---

## ServiceBusOptions (`ServiceBus` section)

Defined in **`MiniCommerce.Messaging`**. Full guide: [`MESSAGING.md`](MESSAGING.md).

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `Enabled` | `false` | `ServiceBus__Enabled` | Use Service Bus vs HTTP fallback |
| `ConnectionString` | `""` | `ServiceBus__ConnectionString` | Local / connection-string auth |
| `FullyQualifiedNamespace` | — | `ServiceBus__FullyQualifiedNamespace` | Azure MI: `*.servicebus.windows.net` |
| `TopicName` | `orders` | `ServiceBus__TopicName` | Integration events topic |
| `SubscriptionName` | `notification-service` | `ServiceBus__SubscriptionName` | Consumer subscription |
| `MaxRetryCount` | `5` | `ServiceBus__MaxRetryCount` | SDK exponential retries |
| `RetryDelaySeconds` | `1` | `ServiceBus__RetryDelaySeconds` | Initial retry delay |
| `MaxRetryDelaySeconds` | `30` | `ServiceBus__MaxRetryDelaySeconds` | Max retry backoff |
| `MaxConcurrentCalls` | `4` | `ServiceBus__MaxConcurrentCalls` | Processor concurrency |
| `MaxAutoLockRenewalMinutes` | `5` | `ServiceBus__MaxAutoLockRenewalMinutes` | Lock renewal |

**Publisher:** Order, Inventory, Payment. **Consumer:** Notification (`BackgroundService`).

Events: `OrderCreated`, `PaymentCompleted`, `InventoryReserved`, `InventoryFailed` (plus existing order status/cancel).

---

## AzureAuthOptions (`AzureAuth` section)

Full guide: [`AZURE-AUTH.md`](AZURE-AUTH.md).

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `PreferManagedIdentity` | `true` in Prod / `false` in Dev | `AzureAuth__PreferManagedIdentity` | Use MI endpoints vs connection strings |
| `ManagedIdentityClientId` | — | `AzureAuth__ManagedIdentityClientId` | Optional user-assigned MI client id |

Provider: `MiniCommerce.AzureAuth` → `DefaultAzureCredential` for Blob, Key Vault, Service Bus, Azure SQL.

---

## KeyVaultOptions (`KeyVault` section)

Full guide: [`KEYVAULT.md`](KEYVAULT.md).

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `Enabled` | `false` | `KeyVault__Enabled` | Load secrets from Key Vault at startup |
| `VaultUri` | — | `KeyVault__VaultUri` | e.g. `https://{vault}.vault.azure.net/` |
| `ReloadIntervalMinutes` | `30` | `KeyVault__ReloadIntervalMinutes` | Periodic secret reload |

**Auth:** shared `DefaultAzureCredential` (`AzureAuth`). Keep `Enabled=false` locally and use User Secrets.

**Loads automatically (secret name → config):** JWT (`Jwt--SigningKey`), connection strings (`ConnectionStrings--*`), Service Bus, Blob Storage, Application Insights, `ApiKeys--Internal`.

---

## DownstreamServicesOptions (`Services` section)

Internal HTTP URLs for Order Service integration (not part of public API surface).

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `Inventory` | `http://localhost:8081` | `Services__Inventory` | Inventory API base URL |
| `Notification` | `http://localhost:8082` | `Services__Notification` | Notification API base URL |
| `HttpClientTimeoutSeconds` | `30` | `Services__HttpClientTimeoutSeconds` | Outbound HTTP timeout |

**Used by:** Order Service only. Docker Compose sets container DNS names.

---

## OpenTelemetryOptions (`OpenTelemetry` section)

Full guide: [`OBSERVABILITY.md`](OBSERVABILITY.md).

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `Enabled` | `true` | `OpenTelemetry__Enabled` | Register tracing/metrics/logging instrumentations |
| `ServiceName` | assembly name | `OpenTelemetry__ServiceName` | Resource `service.name` |
| `OtlpEndpoint` | — | `OpenTelemetry__OtlpEndpoint` | Collector endpoint |
| `OtlpProtocol` | `Grpc` | `OpenTelemetry__OtlpProtocol` | `Grpc` or `HttpProtobuf` |
| `ExcludeHealthChecks` | `true` | `OpenTelemetry__ExcludeHealthChecks` | Skip `/health*` spans |
| `CaptureSqlText` | `false` | `OpenTelemetry__CaptureSqlText` | Include SQL text in spans |
| `Exporters:Console` | Dev `true` | `OpenTelemetry__Exporters__Console` | Console exporter |
| `Exporters:Otlp` | `false` | `OpenTelemetry__Exporters__Otlp` | OTLP exporter |
| `Exporters:AzureMonitor` | `true` | `OpenTelemetry__Exporters__AzureMonitor` | Azure Monitor when App Insights CS is set |

---

## ApplicationInsightsOptions (`ApplicationInsights` section)

| Property | Env variable | Description |
|----------|--------------|-------------|
| `ConnectionString` | `ApplicationInsights__ConnectionString` | Azure Monitor / App Insights |
| `EnableDependencyTracking` | `ApplicationInsights__EnableDependencyTracking` | Dependency telemetry |
| `EnablePerformanceCounters` | `ApplicationInsights__EnablePerformanceCounters` | Performance counters |

Legacy fallback: `Observability:ApplicationInsightsConnectionString`.

---

## Per-service appsettings checklist

Each API `appsettings.json` should include sections relevant to that service:

| Section | Auth | Cart | Catalog | Order | Inventory | Notification | Payment |
|---------|:----:|:----:|:-------:|:-----:|:---------:|:------------:|:-------:|
| ConnectionStrings | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| Database | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| Jwt | ✓ | ✓ | ✓ | ✓ | ✓ | — | ✓ |
| Cors | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| KeyVault | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| ApplicationInsights | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| OpenTelemetry | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| BlobStorage | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| ServiceBus | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Services | — | — | — | ✓ | — | — | — |

---

## Local development

1. Use `appsettings.json` defaults (SQL on `localhost,1433`, JWT dev key, CORS for Vite `5173`).
2. `KeyVault:Enabled=false`, `ServiceBus:Enabled=false`, `BlobStorage:Enabled=false`.
3. Order uses HTTP to reach Notification when Service Bus is disabled.

## Production / Azure

1. Set `KeyVault:Enabled=true` and `VaultUri` (or env vars). Auth: [`AZURE-AUTH.md`](AZURE-AUTH.md) / [`KEYVAULT.md`](KEYVAULT.md).
2. Clear connection strings / JWT signing keys in `appsettings.Production.json`; supply via Key Vault or CSI.
3. Set `AzureAuth:PreferManagedIdentity=true` (default in Production) for SQL, Blob, Service Bus, and Key Vault.
4. Set `ServiceBus:Enabled=true` for Order and Notification.
5. Never commit real secrets — use Key Vault, User Secrets, or sealed env injection.

---

## User Secrets (optional, local)

```bash
dotnet user-secrets set "ConnectionStrings:OrderDB" "Server=..." --project src/OrderService.API
dotnet user-secrets set "Jwt:SigningKey" "your-local-secret" --project src/AuthService.API
```

User Secrets override `appsettings.json` in Development without committing credentials.

---

## Health checks

Every API registers ASP.NET Core health checks via `AddMiniCommerceHealthChecks` / `MapMiniCommerceHealthEndpoints`:

| Endpoint | Role |
|----------|------|
| `/health/live` | Liveness (`self`) |
| `/health/ready` | Readiness: `self` + SQL + Blob + Service Bus + Key Vault when enabled |

Details, JSON schema, and probe examples: [`HEALTHCHECKS.md`](HEALTHCHECKS.md).

---

## Structured logging

All APIs use `ILogger<T>` with Serilog. Shared middleware emits **CorrelationId**, **RequestId**, **TraceId**, **ExecutionTimeMs**, and **Exception**.

See [`LOGGING.md`](LOGGING.md).
