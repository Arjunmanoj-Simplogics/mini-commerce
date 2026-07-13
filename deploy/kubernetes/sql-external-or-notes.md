# Azure SQL for AKS (recommended)

For AKS, prefer **Azure SQL Database** over running SQL Server in the cluster.

## Suggested setup

1. Create an Azure SQL server (firewall allow AKS egress / **private endpoint** preferred).
2. Create **one database per service** (do not share tables across services):

| Database | Service |
|----------|---------|
| `AuthDB` | auth-service |
| `CatalogDB` | catalog-service |
| `CartDB` | cart-service |
| `OrderDB` | order-service |
| `InventoryDB` | inventory-service |
| `NotificationDB` | notification-service |

`payment-service` has **no** database.

3. Put connection strings in Key Vault (CSI) or `secrets.yaml` (from `secrets.yaml.example`).
4. Services create/ensure schema on startup when configured (`Database__AutoMigrate` / `EnsureCreated`).

Full Azure checklist: [`../azure/AZURE-ARCHITECTURE.md`](../azure/AZURE-ARCHITECTURE.md).
