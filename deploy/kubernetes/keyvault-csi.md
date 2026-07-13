# Azure Key Vault CSI Driver integration

## What this enables

Pods mount secrets from **Azure Key Vault** via the Secrets Store CSI Driver. The driver also syncs them into the Kubernetes Secret `mini-commerce-secrets`, which deployments already reference.

## Prerequisites

1. AKS cluster with:
   - `azure-keyvault-secrets-provider` add-on **or** Secrets Store CSI Driver + Azure provider
   - Workload Identity enabled
2. Azure Key Vault with access for the workload identity (`get` on secrets)
3. Secrets created in Key Vault (use `--` instead of `__` in secret names):

| Key Vault secret name | Used as |
|----------------------|---------|
| `ConnectionStrings--OrderDB` | `ConnectionStrings__OrderDB` |
| `ConnectionStrings--InventoryDB` | `ConnectionStrings__InventoryDB` |
| `ConnectionStrings--NotificationDB` | `ConnectionStrings__NotificationDB` |
| `ConnectionStrings--AuthDB` | `ConnectionStrings__AuthDB` |
| `ConnectionStrings--CatalogDB` | `ConnectionStrings__CatalogDB` |
| `ConnectionStrings--CartDB` | `ConnectionStrings__CartDB` |
| `ServiceBus--ConnectionString` | `ServiceBus__ConnectionString` |
| `Jwt--SigningKey` | `Jwt__SigningKey` |

## Apply

1. Edit `service-account.yaml` and `secret-provider-class.yaml` (client ID, vault name, tenant ID).
2. Ensure deployments use `serviceAccountName: mini-commerce-workload` and the CSI volume (see `csi-volume-snippet.yaml`).
3. Apply:

```bash
kubectl apply -f deploy/kubernetes/service-account.yaml
kubectl apply -f deploy/kubernetes/secret-provider-class.yaml
# then redeploy workloads
```

## Local / non-AKS

Apps can still load Key Vault via SDK (`KeyVault:Enabled` + `KeyVault:VaultUri` + `DefaultAzureCredential`) without CSI — Order Service already supports this. CSI is the AKS-native path.

## Provision helper

```powershell
.\scripts\provision-keyvault.ps1 -ResourceGroup rg-mini-commerce -KeyVaultName <unique-kv-name>
```
