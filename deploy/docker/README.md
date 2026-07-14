# Mini Commerce — production Docker images (`deploy/docker/`)

Deployment artifacts only. Application source, solution files, and business logic are unchanged.

Build all .NET images from the **repository root**. Build the frontend with context **`./frontend`**.

---

## Expected image names

| Dockerfile | Image name |
|------------|------------|
| `AuthService.Dockerfile` | `minicommerce/auth-service` |
| `CatalogService.Dockerfile` | `minicommerce/catalog-service` |
| `CartService.Dockerfile` | `minicommerce/cart-service` |
| `InventoryService.Dockerfile` | `minicommerce/inventory-service` |
| `OrderService.Dockerfile` | `minicommerce/order-service` |
| `NotificationService.Dockerfile` | `minicommerce/notification-service` |
| `Frontend.Dockerfile` | `minicommerce/frontend` |

Tag with version or git SHA in CI, e.g. `minicommerce/order-service:1.0.0`.

---

## Build commands

```bash
# From repository root (.NET APIs)
docker build -f deploy/docker/AuthService.Dockerfile         -t minicommerce/auth-service:latest .
docker build -f deploy/docker/CatalogService.Dockerfile      -t minicommerce/catalog-service:latest .
docker build -f deploy/docker/CartService.Dockerfile         -t minicommerce/cart-service:latest .
docker build -f deploy/docker/InventoryService.Dockerfile    -t minicommerce/inventory-service:latest .
docker build -f deploy/docker/OrderService.Dockerfile        -t minicommerce/order-service:latest .
docker build -f deploy/docker/NotificationService.Dockerfile -t minicommerce/notification-service:latest .

# Frontend (context = frontend/)
docker build -f deploy/docker/Frontend.Dockerfile \
  -t minicommerce/frontend:latest \
  ./frontend
```

Azure Container Registry:

```bash
az acr build -r <acr> -f deploy/docker/OrderService.Dockerfile -t order-service:{{.Run.ID}} .
az acr build -r <acr> -f deploy/docker/Frontend.Dockerfile -t frontend:{{.Run.ID}} ./frontend
```

Enable BuildKit locally: `DOCKER_BUILDKIT=1` (default on modern Docker Desktop).

---

## Runtime

| Item | Value |
|------|--------|
| Container port (all) | **8080** |
| API health | `GET /health/live` |
| Frontend health | `GET /healthz` |
| User | non-root (`app` / `nginx`) |
| Config | Environment variables / ConfigMap / Secret |

Example:

```bash
docker run --rm -p 8083:8080 \
  -e ConnectionStrings__AuthDB="..." \
  -e Jwt__SigningKey="..." \
  minicommerce/auth-service:latest
```

Optional frontend build-args: `VITE_API_BASE_URL`, `VITE_AUTH_API_BASE_URL`, … (see `Frontend.Dockerfile`).
