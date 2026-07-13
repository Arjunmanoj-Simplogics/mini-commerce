# Azure architecture checklist — MiniMart / Mini Commerce

Concrete target topology that matches this repo’s microservices and `deploy/kubernetes` manifests.

## Target topology

```text
Browser
  │
  ├─ Static Web Apps / Blob+CDN  →  React storefront (MiniMart)
  │
  └─ Azure Front Door or Application Gateway (TLS / WAF / load balance)
        │
        └─ Azure API Management  (optional but recommended as API Gateway)
              │  routes /api/* → AKS
              ▼
         AKS cluster
           ├─ Ingress Controller (NGINX or AGIC)
           ├─ Deployments: auth, catalog, cart, payment, order, inventory, notification
           ├─ ClusterIP Services (internal only)
           └─ HPA (order, notification, …)
              │
              ├─ Azure SQL  (one database per service)
              ├─ Azure Service Bus  (topic: orders)
              └─ Azure Key Vault  (CSI → pod secrets)
```

| Azure resource | Maps to in this app |
|----------------|---------------------|
| AKS | All microservice pods |
| Load balancer / App Gateway / Front Door | Public HTTPS entry + scale-out |
| API Management (or Ingress alone) | Path-based API Gateway |
| Azure SQL | AuthDB, CatalogDB, CartDB, OrderDB, InventoryDB, NotificationDB |
| Service Bus | Async Order → Notification |
| Key Vault + CSI | Connection strings, JWT signing key |

Payment has **no database**.

---

## 1. Resource group & naming

Suggested names (replace with yours):

| Resource | Example |
|----------|---------|
| Resource group | `rg-minimart-prod` |
| Region | `eastus` (or your region) |
| ACR | `acrminimart` |
| AKS | `aks-minimart` |
| SQL server | `sql-minimart` |
| Service Bus | `sb-minimart` |
| Key Vault | `kv-minimart` |
| APIM | `apim-minimart` |

```bash
az group create -n rg-minimart-prod -l eastus
```

---

## 2. Azure Container Registry + images

Build/push every service image (tags must match your Deployment `image:` fields, or update those YAMLs).

| Image | Dockerfile |
|-------|------------|
| `order-service` | `src/OrderService.API/Dockerfile` |
| `inventory-service` | `src/InventoryService.API/Dockerfile` |
| `notification-service` | `src/NotificationService.API/Dockerfile` |
| `auth-service` | `src/AuthService.API/Dockerfile` |
| `catalog-service` | `src/CatalogService.API/Dockerfile` |
| `cart-service` | `src/CartService.API/Dockerfile` |
| `payment-service` | `src/PaymentService.API/Dockerfile` |

```bash
az acr create -g rg-minimart-prod -n acrminimart --sku Basic
az aks update -g rg-minimart-prod -n aks-minimart --attach-acr acrminimart

# Example build (from repo root)
az acr build -r acrminimart -t order-service:latest -f src/OrderService.API/Dockerfile .
# …repeat for each service
```

Update each `*-deployment.yaml` `image:` to `acrminimart.azurecr.io/<service>:latest` (or use a single overlay later).

---

## 3. Azure SQL (database-per-service)

Create **one** Azure SQL logical server and **six** databases (no shared tables across services).

| Database | Used by |
|----------|---------|
| `AuthDB` | auth-service |
| `CatalogDB` | catalog-service |
| `CartDB` | cart-service |
| `OrderDB` | order-service |
| `InventoryDB` | inventory-service |
| `NotificationDB` | notification-service |

```bash
az sql server create -g rg-minimart-prod -n sql-minimart -l eastus \
  --admin-user sqladmin --admin-password '<StrongPassword>'

# Prefer private endpoint / VNet rules for AKS; for smoke tests only:
# az sql server firewall-rule create ... AllowAzureServices

for db in AuthDB CatalogDB CartDB OrderDB InventoryDB NotificationDB; do
  az sql db create -g rg-minimart-prod -s sql-minimart -n $db --service-objective Basic
done
```

Store connection strings in Key Vault (see §5) using the secret names already expected by CSI:

- `ConnectionStrings--AuthDB`
- `ConnectionStrings--CatalogDB`
- `ConnectionStrings--CartDB`
- `ConnectionStrings--OrderDB`
- `ConnectionStrings--InventoryDB`
- `ConnectionStrings--NotificationDB`

Format:

```text
Server=tcp:sql-minimart.database.windows.net,1433;Database=OrderDB;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False;
```

Services create schema on startup when `Database__AutoMigrate=true` (inventory/order) or `EnsureCreated` (auth/catalog/cart).

Details: [`sql-external-or-notes.md`](../kubernetes/sql-external-or-notes.md)

---

## 4. Azure Service Bus (event bus)

Topic + subscription already match the code and provision script.

| Setting | Value |
|---------|-------|
| Topic | `orders` |
| Subscription (active) | `notification-service` |
| Subscription (reserved) | `inventory-service` (future async stock) |

```powershell
.\scripts\provision-servicebus.ps1 -ResourceGroup rg-minimart-prod -NamespaceName sb-minimart -Location eastus
```

Put the connection string in Key Vault as `ServiceBus--ConnectionString`.

ConfigMap (`configmap.yaml`) already sets:

- `ServiceBus__Enabled=true`
- `ServiceBus__TopicName=orders`
- `ServiceBus__SubscriptionName=notification-service`

**Flow:** Order publishes → topic `orders` → Notification consumes. Local Docker can leave Service Bus off (HTTP fallback).

---

## 5. Key Vault + CSI

```powershell
.\scripts\provision-keyvault.ps1 -ResourceGroup rg-minimart-prod -KeyVaultName kv-minimart -Location eastus
```

Replace `REPLACE_ME` secret values, then:

1. Enable AKS Secrets Store CSI + Azure Key Vault provider.
2. Configure Workload Identity; set `clientID`, `keyvaultName`, `tenantId` in [`secret-provider-class.yaml`](../kubernetes/secret-provider-class.yaml).
3. Grant the workload identity **Get** on secrets.
4. Apply `service-account.yaml` + `secret-provider-class.yaml`.

Synced Kubernetes secret name: `mini-commerce-secrets` (same keys as [`secrets.yaml.example`](../kubernetes/secrets.yaml.example)).

Guide: [`keyvault-csi.md`](../kubernetes/keyvault-csi.md)

---

## 6. AKS cluster

```bash
az aks create -g rg-minimart-prod -n aks-minimart -l eastus \
  --node-count 2 --enable-managed-identity \
  --enable-addons azure-keyvault-secrets-provider \
  --enable-oidc-issuer --enable-workload-identity

az aks get-credentials -g rg-minimart-prod -n aks-minimart
```

Install an ingress controller if not using AGIC:

```bash
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.11.1/deploy/static/provider/cloud/deploy.yaml
```

Public **Azure Load Balancer** is created for the ingress Service — that is your cluster-level load balancer.

---

## 7. API Gateway routing

### Option A — Kubernetes Ingress (already in repo)

[`ingress.yaml`](../kubernetes/ingress.yaml) path-routes to ClusterIP services. Point DNS `api.yourdomain.com` at the ingress LB IP.

| Path prefix | Backend service |
|-------------|-----------------|
| `/api/auth` | auth-service |
| `/api/catalog` | catalog-service |
| `/api/cart` | cart-service |
| `/api/payments` | payment-service |
| `/api/orders` | order-service |
| `/api/inventory` | inventory-service |
| `/api/notifications` | notification-service |
| `/api/events` | notification-service (dev/fallback only; prefer Service Bus in prod) |

### Option B — Azure API Management in front of Ingress (recommended)

Create APIM (Developer/Standard), set backend to `https://api.yourdomain.com` (ingress), and define operations:

| APIM API operation | Backend path |
|--------------------|--------------|
| POST/GET `/auth/*` | `/api/auth/*` |
| GET `/catalog/*` | `/api/catalog/*` |
| `/cart/*` | `/api/cart/*` |
| POST `/payments/charge` | `/api/payments/charge` |
| `/orders/*` | `/api/orders/*` |
| `/inventory/*` | `/api/inventory/*` |

APIM policies to add later: rate limit, CORS for the storefront origin, JWT validate (optional if services already validate JWT).

Frontend should use **one** base URL (APIM or ingress), e.g.:

```env
VITE_API_BASE_URL=https://api.yourdomain.com
VITE_AUTH_API_BASE_URL=https://api.yourdomain.com
VITE_CATALOG_API_BASE_URL=https://api.yourdomain.com
VITE_CART_API_BASE_URL=https://api.yourdomain.com
VITE_PAYMENT_API_BASE_URL=https://api.yourdomain.com
VITE_INVENTORY_API_BASE_URL=https://api.yourdomain.com
VITE_NOTIFICATION_API_BASE_URL=https://api.yourdomain.com
```

(Or refactor the frontend to a single `VITE_GATEWAY_URL` — same host for all.)

Update ConfigMap `Cors__AllowedOrigins__0` to your real storefront origin (Static Web Apps URL).

---

## 8. AKS apply order (this repo)

From `deploy/kubernetes`:

```bash
# 1. Foundation
kubectl apply -f namespace.yaml
kubectl apply -f service-account.yaml
kubectl apply -f secret-provider-class.yaml
kubectl apply -f configmap.yaml
# If not using CSI yet:
# kubectl apply -f secrets.yaml   # copied from secrets.yaml.example

# 2. Workloads (order: data-plane deps before order-service)
kubectl apply -f auth-service-deployment.yaml
kubectl apply -f catalog-service-deployment.yaml
kubectl apply -f cart-service-deployment.yaml
kubectl apply -f payment-service-deployment.yaml
kubectl apply -f inventory-service-deployment.yaml
kubectl apply -f notification-service-deployment.yaml
kubectl apply -f order-service-deployment.yaml

# 3. Networking + scale
kubectl apply -f services.yaml
kubectl apply -f ingress.yaml
kubectl apply -f hpa.yaml
```

Checklist: [`kustomization-notes.md`](../kubernetes/kustomization-notes.md)

---

## 9. Frontend hosting

| Option | Notes |
|--------|-------|
| Azure Static Web Apps | Best fit for Vite/React |
| Storage static website + CDN | Simple + cheap |
| App Service | Also fine |

Build with gateway env vars (§7), deploy `frontend/dist`.

Admin remains the same SPA: `/admin/inventory`, `/admin/orders` (Admin JWT role).

---

## 10. End-to-end verification

1. `GET https://api…/api/catalog` → product list  
2. Register/login → JWT  
3. Add to cart → charge via `/api/payments/charge`  
4. Create order → inventory quantity reserved  
5. Service Bus message on topic `orders` → row in NotificationDB  
6. Admin login → update inventory, list all orders  

Seeded admin (after AuthDB init): `admin@minicommerce.local` / `Admin123!` — change in production.

---

## 11. Security checklist (production)

- [ ] Private endpoint: SQL + Key Vault (+ Service Bus if possible)  
- [ ] No `RootManageSharedAccessKey` long-term — use managed identity / least-privilege SAS  
- [ ] Rotate `Jwt__SigningKey`; same key on Auth, Order, Cart, Catalog, Payment, Inventory  
- [ ] TLS on Front Door / App Gateway / APIM  
- [ ] Lock CORS to storefront origin only  
- [ ] Do not expose `/api/events` publicly if Service Bus is enabled  
- [ ] Restrict Inventory mutate APIs to Admin (already enforced in code)

---

## Local vs Azure

| Concern | Local (Docker Compose) | Azure |
|---------|------------------------|-------|
| Compute | Containers on one host | AKS Deployments |
| SQL | One SQL Server container, many DBs | Azure SQL, many DBs |
| Gateway | Direct ports 8080–8086 | Ingress + optional APIM |
| Events | Service Bus optional / HTTP fallback | Service Bus on |
| Secrets | Compose env | Key Vault CSI |
| UI | `npm run dev` :5173 | Static Web Apps / CDN |
