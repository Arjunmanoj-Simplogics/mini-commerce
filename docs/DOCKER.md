# Mini Commerce — Docker

Production-grade container images for every microservice and the React storefront. **Application logic is unchanged** — only packaging and local orchestration.

---

## Quick start (local)

```bash
docker compose down -v
docker compose up -d --build
```

| Surface | URL |
|---------|-----|
| Storefront | http://localhost:3000 |
| Order Swagger | http://localhost:8080/swagger |
| Inventory | http://localhost:8081 |
| Notification | http://localhost:8082 |
| Auth | http://localhost:8083 |
| Catalog | http://localhost:8084 |
| Cart | http://localhost:8085 |
| Payment | http://localhost:8086 |
| SQL Server | localhost:1433 (sa / `Your_strong_Password123`) |

Health (every API): `GET /health/live`, `/health/ready`, `/health`.

---

## Layout

| Path | Role |
|------|------|
| `src/*/Dockerfile` | One multi-stage Dockerfile per .NET API |
| `frontend/Dockerfile` | Vite build → unprivileged nginx |
| `frontend/nginx.conf` | SPA routing + `/healthz` |
| `docker-compose.yml` | Local SQL + all APIs + frontend |
| `.dockerignore` | Keeps API build context small |
| `frontend/.dockerignore` | Keeps frontend context small |

---

## Common .NET Dockerfile pattern (explain every stage)

Each API Dockerfile (`Auth`, `Cart`, `Catalog`, `Inventory`, `Notification`, `Order`, `Payment`) follows the same production template.

### Stage `restore` — `mcr.microsoft.com/dotnet/sdk:9.0`

1. Copies only `Directory.Build.props`, `global.json`, `MiniCommerce.sln`, and the **project graph `.csproj` files**.
2. Runs `dotnet restore` for that API.
3. **Optimized layer caching:** changing C# source does not invalidate the restore layer; only csproj/NuGet changes do.
4. Always includes shared packages: `MiniCommerce.AzureAuth`, `Contracts`, `Storage`, `Messaging`, `BuildingBlocks` (BuildingBlocks depends on them).

### Stage `publish` — continues from `restore`

1. `COPY . .` brings full sources (filtered by `.dockerignore`).
2. `dotnet publish -c Release --no-restore /p:UseAppHost=false` → `/app/publish`.
3. Framework-dependent publish (no bundled native host) keeps the runtime image smaller.

### Stage `final` — `mcr.microsoft.com/dotnet/aspnet:9.0`

1. **Small runtime image** (no SDK).
2. **Environment variables:** `ASPNETCORE_URLS=http://+:8080`, `ASPNETCORE_ENVIRONMENT=Production`, `DOTNET_EnableDiagnostics=0`.
3. Installs **curl** only for HEALTHCHECK, then deletes apt lists.
4. **Non-root user:** official `app` account (`COPY --chown=app:app` + `USER app`).
5. **Expose 8080** (same as AKS manifests; compose maps host ports).
6. **HEALTHCHECK** against `http://127.0.0.1:8080/health/live`.
7. `ENTRYPOINT ["dotnet", "<Service>.API.dll"]`.

Compose overrides `ASPNETCORE_ENVIRONMENT=Development` so Swagger works locally.

---

## Per-service Dockerfiles

| Dockerfile | Entry assembly | Extra restore projects |
|------------|----------------|------------------------|
| `src/OrderService.API/Dockerfile` | `OrderService.API.dll` | Domain, Application, Infrastructure |
| `src/InventoryService.API/Dockerfile` | `InventoryService.API.dll` | Domain, Application, Infrastructure |
| `src/NotificationService.API/Dockerfile` | `NotificationService.API.dll` | Domain, Application, Infrastructure |
| `src/AuthService.API/Dockerfile` | `AuthService.API.dll` | API only (+ shared) |
| `src/CatalogService.API/Dockerfile` | `CatalogService.API.dll` | API (+ Storage via shared graph) |
| `src/CartService.API/Dockerfile` | `CartService.API.dll` | API only (+ shared) |
| `src/PaymentService.API/Dockerfile` | `PaymentService.API.dll` | API (+ Messaging/Contracts via shared) |

Build one service from repo root:

```bash
docker build -f src/OrderService.API/Dockerfile -t minicommerce-order:local .
```

---

## Frontend Dockerfile (`frontend/Dockerfile`)

### Stage `deps` — `node:20-alpine`

- Copies `package.json` + `package-lock.json` only, then `npm ci`.
- Cached until lockfile changes.

### Stage `build` — continues from `deps`

- Copies frontend sources.
- Accepts **build-args** for all `VITE_*` API base URLs (baked into the SPA at build time).
- Runs `npm run build` → `dist/`.

### Stage `final` — `nginxinc/nginx-unprivileged:1.27-alpine`

- **Non-root** nginx (listens on **8080**).
- Serves `dist/` with `nginx.conf` (SPA `try_files`, long-cache `/assets/`, `/healthz`).
- **HEALTHCHECK** via `wget` to `/healthz`.
- Small alpine-based image (no Node runtime).

```bash
docker build -f frontend/Dockerfile -t minicommerce-frontend:local ./frontend
```

---

## docker-compose.yml

- **SQL Server 2022** with named volume + healthcheck.
- All seven APIs: build from Dockerfiles, host ports `8080–8086` → container `8080`.
- **Frontend** on `http://localhost:3000`.
- Shared env (YAML anchor): JWT, CORS (5173 + 3000), SQL retries, MI off, OTel exporters off.
- Internal DNS: Order → `http://inventory-service:8080`, `http://notification-service:8080`.
- Optional Azure Service Bus: `SERVICEBUS_ENABLED`, `SERVICEBUS_CONNECTION_STRING`.

---

## Production notes

1. Prefer ACR builds: `az acr build -f src/<svc>/Dockerfile -t <svc>:tag .`
2. Inject secrets via env / Key Vault — never bake connection strings into images.
3. Keep container listen port **8080** to match Kubernetes manifests.
4. Frontend must be rebuilt when API public URLs change (`VITE_*` build-args).
