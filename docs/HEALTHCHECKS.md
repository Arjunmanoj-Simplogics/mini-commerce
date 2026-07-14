# Mini Commerce — Health Checks

ASP.NET Core Health Checks are registered once in `MiniCommerce.BuildingBlocks` and wired by every API.

**Registration (startup):**

```csharp
builder.Services.AddMiniCommerceHealthChecks(builder.Configuration, sqlConnectionString);
// ...
app.MapMiniCommerceHealthEndpoints();
```

`sqlConnectionString` is optional (omit for Payment Service, which has no database).

---

## Endpoints

| Path | Purpose | HTTP status |
|------|---------|-------------|
| `GET /health` | All registered checks | `200` Healthy/Degraded, `503` Unhealthy |
| `GET /health/live` | Liveness (process up) | same |
| `GET /health/ready` | Readiness (dependencies) | same |
| `GET /api/health*` | Legacy aliases for existing Docker/K8s manifests | same |

All endpoints are **anonymous** (no JWT) so Docker `HEALTHCHECK` and Kubernetes probes can call them.

---

## Response format

Responses are `application/json` with camelCase properties:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0456123",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": "Process is running.",
      "duration": "00:00:00.0000123",
      "exception": null,
      "tags": ["live", "ready"]
    },
    {
      "name": "sql",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0321000",
      "exception": null,
      "tags": ["ready"]
    }
  ]
}
```

| Overall `status` | Meaning | Status code |
|------------------|---------|-------------|
| `Healthy` | All matching checks passed | `200` |
| `Degraded` | Soft failure (e.g. Blob enabled but service not registered) | `200` |
| `Unhealthy` | Hard failure (SQL down, Key Vault unreachable, …) | `503` |

---

## Checks

| Name | Tags | When registered | What it verifies |
|------|------|-----------------|------------------|
| `self` | `live`, `ready` | Always | Process is running |
| `sql` | `ready` | When a SQL connection string is passed to `AddMiniCommerceHealthChecks` | Opens SQL Server and runs health query (`AspNetCore.HealthChecks.SqlServer`) |
| `blob` | `ready` | `BlobStorage:Enabled=true` | Resolves blob URL + ensures container exists |
| `servicebus` | `ready` | `ServiceBus:Enabled=true` | Creates `ServiceBusClient` and topic sender |
| `keyvault` | `ready` | `KeyVault:Enabled=true` | Connects with `DefaultAzureCredential` and lists secret properties (max 1) |

### Liveness vs readiness

- **`/health/live`** — only checks tagged `live` → currently `self`. Restart the container if this fails.
- **`/health/ready`** — checks tagged `ready` → `self` plus SQL / Blob / Service Bus / Key Vault when enabled. Stop sending traffic if this fails.

### Local vs Azure

| Check | Local (typical) | Azure / Production |
|-------|-----------------|--------------------|
| SQL | Registered (connection string in `appsettings.json`) | Registered via env / Key Vault / CSI |
| Blob | Skipped (`BlobStorage:Enabled=false`) | Registered when enabled + MI / connection string |
| Service Bus | Skipped (`ServiceBus:Enabled=false`) | Registered when enabled |
| Key Vault | Skipped (`KeyVault:Enabled=false`) | Registered when `KeyVault:Enabled=true` |

---

## Per-service registration

| Service | SQL check | Blob | Service Bus | Key Vault |
|---------|:---------:|:----:|:-----------:|:---------:|
| Auth | AuthDB | if enabled | if enabled | if enabled |
| Cart | CartDB | if enabled | if enabled | if enabled |
| Catalog | CatalogDB | if enabled | if enabled | if enabled |
| Order | OrderDB | if enabled | if enabled | if enabled |
| Inventory | InventoryDB | if enabled | if enabled | if enabled |
| Notification | NotificationDB | if enabled | if enabled | if enabled |
| Payment | — | if enabled | if enabled | if enabled |

Implementation files:

| File | Role |
|------|------|
| `HealthCheckExtensions.cs` | `AddMiniCommerceHealthChecks` + `MapMiniCommerceHealthEndpoints` |
| `HealthCheckResponseWriter.cs` | JSON response writer |
| `BlobStorageHealthCheck.cs` | Blob readiness |
| `ServiceBusHealthCheck.cs` | Service Bus readiness |
| `KeyVaultHealthCheck.cs` | Key Vault readiness |

---

## Probe examples

```bash
# Liveness (Docker HEALTHCHECK / K8s livenessProbe)
curl -s http://localhost:8080/health/live

# Readiness (K8s readinessProbe)
curl -s http://localhost:8080/health/ready

# Full report
curl -s http://localhost:8080/health
```

Kubernetes manifests under `deploy/kubernetes/` currently probe `/api/health/live` and `/api/health/ready` (legacy paths remain supported). New digests may use `/health/live` and `/health/ready` equivalently.

---

## Related configuration

See [`CONFIGURATION.md`](CONFIGURATION.md) for:

- `ConnectionStrings__*`
- `BlobStorage__Enabled`
- `ServiceBus__Enabled`
- `KeyVault__Enabled` / `KeyVault__VaultUri`
