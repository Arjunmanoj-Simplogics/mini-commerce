# Mini Commerce — Azure Blob Storage

Azure Blob Storage is encapsulated in the **`MiniCommerce.Storage`** class library. Controllers depend on **`IBlobStorageService`** only — the Azure SDK is never exposed above the storage layer.

**Catalog Service** is the sole consumer: product images are uploaded to blob storage and **only the blob URL** is persisted in SQL (`Product.ImageUrl`).

---

## Project layout

| File | Purpose |
|------|---------|
| `Options/BlobStorageOptions.cs` | Strongly typed `BlobStorage` configuration section |
| `Abstractions/IBlobStorageService.cs` | Application contract: Upload, Download, Delete, GetBlobUrl, EnsureContainerExists |
| `Abstractions/IBlobContainerClient.cs` | Testable container abstraction (hides SDK) |
| `Abstractions/IBlobContainerClientFactory.cs` | Factory for container clients (mock in tests) |
| `BlobStorageService.cs` | Production `IBlobStorageService` using `IOptions<BlobStorageOptions>` + `ILogger` |
| `Internal/AzureBlobContainerClient.cs` | Azure SDK adapter + factory with retry policies |
| `DependencyInjection/BlobStorageServiceRegistrar.cs` | Instance-based DI registration |

---

## Registration

```csharp
builder.Services.AddMiniCommerceOptions(builder.Configuration); // binds BlobStorageOptions
new BlobStorageServiceRegistrar().Register(builder.Services, builder.Configuration);
```

`IBlobStorageService` is registered **only when** `BlobStorage:Enabled=true`.

---

## Configuration (`BlobStorage` section)

| Property | Default | Env variable | Description |
|----------|---------|--------------|-------------|
| `Enabled` | `false` | `BlobStorage__Enabled` | Register blob service |
| `ConnectionString` | — | `BlobStorage__ConnectionString` | **Local dev only** |
| `ServiceUri` | — | `BlobStorage__ServiceUri` | Azure blob endpoint + MI |
| `AccountName` | — | `BlobStorage__AccountName` | Alternative to ServiceUri |
| `ContainerName` | `product-images` | `BlobStorage__ContainerName` | Upload container |
| `MaxRetryCount` | `5` | `BlobStorage__MaxRetryCount` | SDK exponential retries |
| `RetryDelaySeconds` | `1` | `BlobStorage__RetryDelaySeconds` | Initial retry delay |
| `MaxRetryDelaySeconds` | `30` | `BlobStorage__MaxRetryDelaySeconds` | Max retry backoff |
| `NetworkTimeoutSeconds` | `60` | `BlobStorage__NetworkTimeoutSeconds` | Per-attempt network timeout |

### Authentication

| Environment | Configuration |
|-------------|---------------|
| **Local** | `ConnectionString` (Azurite or dev storage account) |
| **Azure / Production** | `ServiceUri` or `AccountName` + **Managed Identity** (`DefaultAzureCredential` via [`AZURE-AUTH.md`](AZURE-AUTH.md)) |

Never set `ConnectionString` in production manifests.

---

## API usage (Catalog)

Existing endpoint (unchanged):

```http
POST /api/catalog/{id}/image
Authorization: Bearer <admin-jwt>
Content-Type: multipart/form-data
```

Flow:

1. Controller receives `IFormFile`
2. Calls `IBlobStorageService.UploadAsync(...)`
3. Stores returned **URL string** in `Product.ImageUrl`
4. Saves entity via EF Core — **no file bytes in SQL**

---

## Operations

| Method | Description |
|--------|-------------|
| `UploadAsync` | Upload stream → returns absolute blob URL |
| `DownloadAsync` | Returns readable `Stream` (caller disposes) |
| `DeleteAsync` | Deletes blob if present |
| `GetBlobUrl` | URI without transfer |
| `EnsureContainerExistsAsync` | Creates container when missing (health + first upload) |

All methods are **async** and use **ILogger** for structured logs.

---

## Testability

- Mock **`IBlobStorageService`** in controller/service tests
- Mock **`IBlobContainerClientFactory`** / **`IBlobContainerClient`** for storage unit tests
- Azure SDK types stay in `Internal/` — not visible to consumers

---

## Health check

When `BlobStorage:Enabled=true`, `/health/ready` includes a `blob` check that calls `EnsureContainerExistsAsync`.

See [`HEALTHCHECKS.md`](HEALTHCHECKS.md).

---

## Local development

1. Keep `BlobStorage:Enabled=false` to run without storage (seed URLs use external images).
2. To test uploads locally:
   - Run [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) or a dev storage account
   - Set `BlobStorage:Enabled=true` and `BlobStorage:ConnectionString`
   - `POST /api/catalog/{id}/image` with an admin JWT

---

## Related

- [`CONFIGURATION.md`](CONFIGURATION.md) — full configuration reference
- [`LOGGING.md`](LOGGING.md) — structured logging
