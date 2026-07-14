# MiniMart — Mini Commerce Microservices

.NET 9 microservices with a React storefront, JWT auth, Azure-ready configuration (SQL, Service Bus, Blob, Key Vault, Application Insights), and AKS manifests.

## Services

| Service | Local port | Database | Notes |
|---------|------------|----------|-------|
| Order | 8080 | OrderDB | JWT create; Service Bus publisher |
| Inventory | 8081 | InventoryDB | Public read; Admin write |
| Notification | 8082 | NotificationDB | Service Bus consumer |
| Auth | 8083 | AuthDB | Register / login JWT |
| Catalog | 8084 | CatalogDB | Products; optional Blob image upload |
| Cart | 8085 | CartDB | Per-user cart (JWT) |
| Payment | 8086 | — | Mock charge (cards ending in `0000` fail) |

## Shop flow

1. Browse the storefront (`/` / `/shop`)
2. Sign in → add to cart → mock payment (`Payment`)
3. On success, orders are created (`Order`) → stock reserved (`Inventory`) → event published (`Service Bus` → `Notification`)

**Admin:** `/admin/inventory`, `/admin/orders`  
Seeded admin: `admin@minicommerce.local` / `Admin123!`

---

## Local setup

### Prerequisites

- .NET SDK 9 (`global.json` pins `9.0.100`)
- Docker Desktop (for Compose / SQL Server)
- Node.js 20+ (frontend)

### Docker Compose (all APIs + SQL + storefront)

```bash
docker compose down -v
docker compose up -d --build
```

| Surface | URL |
|---------|-----|
| Storefront | http://localhost:3000 |
| APIs | http://localhost:8080–8086 (Swagger in Development) |

Health:

- `GET /health`, `/health/live`, `/health/ready` (JSON; anonymous)
- Legacy: `/api/health*` (same checks — used by current K8s manifests)

Full probe docs: [`docs/HEALTHCHECKS.md`](docs/HEALTHCHECKS.md).  
Dockerfiles & compose detail: [`docs/DOCKER.md`](docs/DOCKER.md).

### Frontend

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

### Solution build (without Docker)

```bash
dotnet restore MiniCommerce.sln
dotnet build MiniCommerce.sln
```

Point connection strings at a local SQL Server (defaults in each `appsettings.json` use `localhost,1433`).

---

## Docker build / run (single service)

Example — Order Service:

```bash
docker build -f src/OrderService.API/Dockerfile -t minicommerce-order:local .
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__OrderDB="Server=host.docker.internal,1433;Database=OrderDB;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False" \
  -e Jwt__SigningKey="MiniCommerce_Dev_Signing_Key_Change_In_Production_32chars" \
  -e Cors__AllowedOrigins__0="http://localhost:5173" \
  minicommerce-order:local
```

Images are multi-stage (SDK restore/publish → `aspnet:9.0`), run as non-root `app`, listen on **8080**, and `HEALTHCHECK` `/health/live`. Frontend: `frontend/Dockerfile` (Node build → unprivileged nginx). See [`docs/DOCKER.md`](docs/DOCKER.md).

---

## Configuration

Full reference for every setting, options class, and environment variable: [`docs/CONFIGURATION.md`](docs/CONFIGURATION.md).

All services register strongly typed options via `AddMiniCommerceOptions()` in `MiniCommerce.BuildingBlocks` (`SqlOptions`, `ConnectionStringsOptions`, `JwtOptions`, `BlobStorageOptions`, `ServiceBusOptions`, `KeyVaultOptions`, `CorsOptions`, `OpenTelemetryOptions`). Azure auth (Dev connection strings / Prod Managed Identity + `DefaultAzureCredential`): [`docs/AZURE-AUTH.md`](docs/AZURE-AUTH.md). Observability: [`docs/OBSERVABILITY.md`](docs/OBSERVABILITY.md). Blob: [`docs/STORAGE.md`](docs/STORAGE.md). Service Bus: [`docs/MESSAGING.md`](docs/MESSAGING.md).


---

## Required environment variables

ASP.NET Core maps nested config with `__`. Prefer **User Secrets** / env / Key Vault — never commit real secrets.

| Variable | Used by | Description |
|----------|---------|-------------|
| `ConnectionStrings__OrderDB` | Order | Azure SQL / SQL Server |
| `ConnectionStrings__InventoryDB` | Inventory | |
| `ConnectionStrings__NotificationDB` | Notification | |
| `ConnectionStrings__AuthDB` | Auth | |
| `ConnectionStrings__CatalogDB` | Catalog | |
| `ConnectionStrings__CartDB` | Cart | |
| `Database__CommandTimeoutSeconds` | SQL services | EF command timeout (default `30`) |
| `Database__MaxRetryCount` | SQL services | `EnableRetryOnFailure` retries (default `3`) |
| `Database__AutoMigrate` | SQL services | Startup migrate / ensure-created |
| `Jwt__Issuer` / `Jwt__Audience` / `Jwt__SigningKey` | Auth + APIs | Symmetric JWT |
| `Cors__AllowedOrigins__0` | All APIs | Frontend origin(s) |
| `ServiceBus__Enabled` | Order, Notification | `true` to use Azure Service Bus |
| `ServiceBus__ConnectionString` | Order, Notification | Local / connection-string auth |
| `ServiceBus__FullyQualifiedNamespace` | Order, Notification | Azure MI: `xxx.servicebus.windows.net` |
| `ServiceBus__TopicName` | Order, Notification | Default `orders` |
| `ServiceBus__SubscriptionName` | Notification | Default `notification-service` |
| `BlobStorage__Enabled` | Catalog | Enable Azure Blob uploads |
| `BlobStorage__ConnectionString` | Catalog | Local only |
| `BlobStorage__ServiceUri` / `BlobStorage__AccountName` | Catalog | Azure + Managed Identity |
| `BlobStorage__ContainerName` | Catalog | Default `product-images` |
| `KeyVault__Enabled` | All APIs | Load secrets from Key Vault |
| `KeyVault__VaultUri` | All APIs | `https://{vault}.vault.azure.net/` |
| `ApplicationInsights__ConnectionString` | All APIs | Azure Monitor / App Insights |
| `Services__Inventory` / `Services__Notification` | Order | Internal HTTP base URLs |

Catalog image upload (Admin JWT): `POST /api/catalog/{id}/image` (multipart file) stores the **blob URL** in `Product.ImageUrl`.

---

## Azure deployment prerequisites

Infrastructure is assumed to exist (Bicep/IaC is out of this repo’s build scope). Application consumption expects:

| Azure service | How the app uses it |
|---------------|---------------------|
| **AKS** | Deployments under `deploy/kubernetes/` |
| **ACR** | Push images built from each service Dockerfile |
| **Azure SQL** | One database per service; connection strings via CSI/Key Vault/env |
| **Service Bus** | Topic `orders`, subscription `notification-service` |
| **Blob Storage** | Container for catalog images (Managed Identity) |
| **Key Vault** | Secrets; app uses `DefaultAzureCredential` when `KeyVault:Enabled=true` |
| **Application Insights** + **Log Analytics** | `ApplicationInsights__ConnectionString` |
| **Managed Identity** / Workload Identity | No client secrets in Azure config files |
| **RBAC** | MI roles: SQL AAD, Storage Blob Data Contributor, Service Bus Data Sender/Receiver, Key Vault Secrets User |

Detailed checklist: [`deploy/azure/AZURE-ARCHITECTURE.md`](deploy/azure/AZURE-ARCHITECTURE.md)

Also:

- Key Vault CSI: `deploy/kubernetes/keyvault-csi.md`, `scripts/provision-keyvault.ps1`
- Service Bus: `scripts/provision-servicebus.ps1`
- SQL notes: `deploy/kubernetes/sql-external-or-notes.md`

### Local vs Azure auth

| Resource | Local | Azure |
|----------|-------|--------|
| Key Vault | `KeyVault:Enabled=false`; use appsettings / User Secrets | `Enabled=true` + **Managed Identity**; secrets for JWT, SQL, Service Bus, Blob, App Insights |

Full Key Vault guide: [`docs/KEYVAULT.md`](docs/KEYVAULT.md).
| Service Bus | Connection string | `FullyQualifiedNamespace` + Managed Identity |
| Blob | Connection string | `ServiceUri` / `AccountName` + Managed Identity |
| App Insights | Leave empty | Set connection string from Key Vault / env |

---

## Observability & health

- **Logging:** `ILogger<T>` + Serilog. Structured fields: `CorrelationId`, `RequestId`, `TraceId`, `SpanId`, `ExecutionTimeMs`, `Exception`. Header `X-Correlation-ID` is accepted/propagated. Details: [`docs/LOGGING.md`](docs/LOGGING.md).
- **Telemetry:** OpenTelemetry (traces, metrics, logs) via `AddMiniCommerceTelemetry` — Console / OTLP / optional Azure Monitor. Details: [`docs/OBSERVABILITY.md`](docs/OBSERVABILITY.md).
- **Health checks:** `/health/live` (liveness), `/health/ready` (SQL, Service Bus, Blob, Key Vault when enabled). Details: [`docs/HEALTHCHECKS.md`](docs/HEALTHCHECKS.md).
- **Graceful shutdown:** `HostOptions.ShutdownTimeout` (default 30s; Notification 45s); Service Bus processor stops on SIGTERM. Kestrel limits + `ASPNETCORE_URLS`. Details: [`docs/KUBERNETES-READY.md`](docs/KUBERNETES-READY.md).
- **HTTPS forwarding:** `X-Forwarded-For` / `Proto` / `Host` for ingress; HTTPS redirection off by default.
