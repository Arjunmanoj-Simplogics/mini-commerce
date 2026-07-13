# MiniMart — Mini Commerce Microservices

.NET 9 microservices with a **glass storefront**, **mock payments**, **JWT Auth**, **Catalog**, **Cart**, **Azure Service Bus**, **Key Vault CSI**, and **AKS** manifests.

## Services

| Service | Local port | Database | Notes |
|---------|------------|----------|-------|
| Order | 8080 | OrderDB | Create requires JWT; admin lists all; customers use `/mine` |
| Inventory | 8081 | InventoryDB | Public read; **Admin** create/update/delete |
| Notification | 8082 | NotificationDB | Service Bus consumer |
| Auth | 8083 | AuthDB | Register/login JWT |
| Catalog | 8084 | CatalogDB | Product listing (admin write) |
| Cart | 8085 | CartDB | Per-user cart (JWT) |
| **Payment** | 8086 | — | Mock charge (cards ending in `0000` fail) |

## Shop flow

1. Browse the glass storefront (`/` / `/shop`)
2. Sign in → add to cart → **mock payment** (`Payment`)
3. On success, orders are created (`Order`) → stock reserved (`Inventory`) → event published (`Service Bus` → `Notification`)

**Admin:** `/admin/inventory` (update stock) and `/admin/orders` (all orders).  
Seeded admin: `admin@minicommerce.local` / `Admin123!`

## Azure Key Vault CSI

- Manifests: `deploy/kubernetes/secret-provider-class.yaml`, `service-account.yaml`
- Guide: `deploy/kubernetes/keyvault-csi.md`
- Provision: `scripts/provision-keyvault.ps1`

## Azure Service Bus

- Topic `orders`, subscription `notification-service`
- Provision: `scripts/provision-servicebus.ps1`
- Toggle: `SERVICEBUS_ENABLED` + `SERVICEBUS_CONNECTION_STRING`

## Docker Compose

```bash
docker compose down -v
docker compose up -d --build
```

Swagger: `8080`–`8086`.

## Frontend

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

Storefront: Home, Shop, Cart, Checkout (mock pay), My orders.  
Admin: Inventory update, All orders.

## AKS / Azure

Concrete Azure checklist (API Gateway, Load Balancer, Azure SQL, Service Bus, Key Vault, apply order):

**[`deploy/azure/AZURE-ARCHITECTURE.md`](deploy/azure/AZURE-ARCHITECTURE.md)**

Also: `deploy/kubernetes/kustomization-notes.md`, `sql-external-or-notes.md`, `keyvault-csi.md`.

Prefer **Azure SQL** (one DB per service). Ingress routes include `/api/payments`.
