using MiniCommerce.Storage.Options;

namespace MiniCommerce.Storage.Abstractions;

/// <summary>
/// Creates <see cref="IBlobContainerClient"/> instances from <see cref="BlobStorageOptions"/>.
/// Injectable for unit tests (mock the factory instead of Azure SDK types).
/// </summary>
public interface IBlobContainerClientFactory
{
    /// <summary>
    /// Builds a container client using connection string (local) or Managed Identity (Azure).
    /// </summary>
    IBlobContainerClient Create(BlobStorageOptions options);
}
