# Mini Commerce — Azure Authentication (Managed Identity)

Single credential strategy for all Azure SDKs: **`DefaultAzureCredential`** via `MiniCommerce.AzureAuth`.

| Environment | Auth model | Blob / Service Bus / SQL |
|-------------|------------|--------------------------|
| **Development** | Connection strings (SQL auth, storage/SB keys) | `ConnectionString` / full SQL CS with user+password |
| **Production** | Managed Identity (via `DefaultAzureCredential`) | Service URI / FQ namespace / `Active Directory Default` SQL |

Architecture, controllers, repositories, and domain logic are unchanged — only how Azure clients authenticate.

---

## Shared provider (`MiniCommerce.AzureAuth`)

| Type | Role |
|------|------|
| `IAzureCredentialProvider` | Single `TokenCredential` + `PreferManagedIdentity` flag |
| `AzureCredentialProvider` | Builds one `DefaultAzureCredential` (excludes interactive browser) |
| `AzureCredentialBootstrap` | Same credential **before DI** (Key Vault config source) |
| `AddMiniCommerceAzureCredential()` | DI registration (idempotent `TryAdd`) |

```csharp
builder.Services.AddMiniCommerceAzureCredential(builder.Configuration);
builder.AddKeyVaultConfiguration();
```

Every API registers this early. Blob / Service Bus registrars and health checks also call it so DI never misses the provider.

---

## Configuration (`AzureAuth`)

| Property | Env | Description |
|----------|-----|-------------|
| `PreferManagedIdentity` | `AzureAuth__PreferManagedIdentity` | `null` → **true** in Production, **false** in Development |
| `ManagedIdentityClientId` | `AzureAuth__ManagedIdentityClientId` | Optional user-assigned / workload identity client id |

**appsettings.json (Development):**

```json
"AzureAuth": {
  "PreferManagedIdentity": false,
  "ManagedIdentityClientId": ""
}
```

**appsettings.Production.json:**

```json
"AzureAuth": {
  "PreferManagedIdentity": true,
  "ManagedIdentityClientId": ""
}
```

`Database:UseManagedIdentity` can override SQL-only behaviour; otherwise SQL follows `AzureAuth:PreferManagedIdentity`.

---

## Service wiring

### Blob Storage (`MiniCommerce.Storage`)
- Dev: `BlobStorage:ConnectionString`
- Prod MI: `ServiceUri` or `AccountName` + `IAzureCredentialProvider.Credential`

### Service Bus (`MiniCommerce.Messaging`)
- Dev: `ServiceBus:ConnectionString`
- Prod MI: `FullyQualifiedNamespace` + credential

### Key Vault (`BuildingBlocks`)
- Always `DefaultAzureCredential` via `AzureCredentialBootstrap` when `KeyVault:Enabled=true`
- Prefer CLI / VS locally; MI in AKS/App Service

### Azure SQL (`AzureSqlExtensions`)
- Dev: connection string as-is (SQL auth)
- Prod MI: rewrite to `Authentication=Active Directory Default`, clear User/Password (Microsoft.Data.SqlClient uses DefaultAzureCredential)

---

## Production IAM checklist

1. Enable system-assigned (or user-assigned) MI on the compute (AKS workload identity / App Service / Container Apps).
2. Grant roles: **Storage Blob Data Contributor**, **Azure Service Bus Data Owner/Sender/Receiver**, **Key Vault Secrets User**, Azure SQL AAD admin + database user mapped to the MI.
3. Set URIs/namespaces (not keys) in config / Key Vault.
4. Leave connection-string secrets empty in Production appsettings.

---

## What was removed / consolidated

- Per-service `new DefaultAzureCredential()` / separate `ManagedIdentityCredential` branches
- `KeyVault:UseManagedIdentity` as the auth switch (obsolete; use `AzureAuth`)
- Duplicate credential construction in Blob, Service Bus, Key Vault, and health checks
