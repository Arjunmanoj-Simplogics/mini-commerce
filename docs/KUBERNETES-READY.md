# Mini Commerce — Kubernetes application readiness

**No manifests in this document** — only how the **application** is prepared to run on Kubernetes (AKS). Deploy YAMLs live separately under `deploy/kubernetes/` when you choose to apply them.

Business APIs, domain rules, and controllers are unchanged except Payment storage (same charge/GET contract, distributed cache for replicas).

---

## Checklist vs requirements

| Requirement | Status |
|-------------|--------|
| Config from environment variables | Production placeholders empty; secrets/URLs via `Env` / Key Vault |
| Graceful shutdown + SIGTERM | `HostOptions.ShutdownTimeout` + Service Bus `StopAsync` |
| Kestrel configured | Limits, no Server header; bind via `ASPNETCORE_URLS` |
| `/health/live` & `/health/ready` | Mapped on every API (`MapMiniCommerceHealthEndpoints`) |
| No localhost in Production | Cleared Production appsettings + empty option defaults |
| Forwarded headers | `X-Forwarded-For` / `Proto` / `Host` before auth |
| Multiple replicas / HPA | SQL-backed services; Payment uses Redis when configured |

---

## Environment variables (Production)

Set these from ConfigMaps / Secrets (examples):

| Variable | Purpose |
|----------|---------|
| `ASPNETCORE_URLS` | `http://+:8080` (container port) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__*DB` | Azure SQL (or empty + MI rewriting) |
| `ConnectionStrings__Redis` | Payment multi-replica cache (optional but recommended) |
| `Jwt__SigningKey` | ≥32 chars (required outside Development) |
| `Cors__AllowedOrigins__0` | Storefront origin (`https://shop.example.com`) |
| `Services__Inventory` | `http://inventory-service:8080` |
| `Services__Notification` | `http://notification-service:8080` |
| `Hosting__ShutdownTimeoutSeconds` | Align with `terminationGracePeriodSeconds` − buffer |
| `Hosting__UseHttpsRedirection` | Keep `false` when TLS terminates at ingress |
| `AzureAuth__PreferManagedIdentity` | `true` |
| `ServiceBus__FullyQualifiedNamespace` | MI namespace |
| `KeyVault__Enabled` / `KeyVault__VaultUri` | Optional overlay after env |

---

## Pipeline order (every API)

1. `AddMiniCommerceAksHosting()` — Kestrel + shutdown + forwarded-headers options  
2. `UseMiniCommerceForwardedHeaders()` — first middleware of note  
3. `UseMiniCommerceHttpsRedirection()` — no-op unless explicitly enabled  
4. `MapMiniCommerceHealthEndpoints()` — `/health/live`, `/health/ready` (anonymous)

---

## Horizontal scaling notes

- **Auth, Cart, Catalog, Order, Inventory, Notification:** shared Azure SQL — safe behind HPA.  
- **Payment:** set `ConnectionStrings__Redis` so charge/GET work across pods. Without Redis, memory cache is **per-pod** (local/dev only).  
- **Notification:** increase `Hosting__ShutdownTimeoutSeconds` (default 45 in Production) so the Service Bus processor drains on SIGTERM.  
- Prefer **stateless** pods: no sticky sessions required when Redis + SQL are shared.

---

## Probes (for when you write manifests)

```text
livenessProbe:  GET /health/live
readinessProbe: GET /health/ready
```

Legacy paths `/api/health*` remain for older manifests.

---

## Implementation files

| File | Change |
|------|--------|
| `Hosting/AksHostingExtensions.cs` | Kestrel hardening, SIGTERM timeout, forwarded headers, optional HTTPS |
| `Configuration/HostingOptions.cs` | Env-bound hosting settings |
| `Configuration/CorsOptions` + CORS extensions | No Production localhost origins |
| `Configuration/DownstreamServicesOptions.cs` | Empty defaults; fail-fast absolute URLs |
| `Auth/JwtAuthenticationExtensions.cs` | Require real SigningKey outside Development |
| `Configuration/DistributedCacheServiceCollectionExtensions.cs` | Redis or memory cache |
| `PaymentService` `IPaymentStore` | Replica-safe lookups |
| `appsettings.Production.json` (all APIs) | Empty secrets/URLs + Hosting section |

Related: [`HEALTHCHECKS.md`](HEALTHCHECKS.md), [`DOCKER.md`](DOCKER.md), [`CONFIGURATION.md`](CONFIGURATION.md), [`AZURE-AUTH.md`](AZURE-AUTH.md).
