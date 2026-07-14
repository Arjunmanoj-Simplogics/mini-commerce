namespace MiniCommerce.Storage.Abstractions;

/// <summary>
/// Testable abstraction over a single blob container. Hides Azure SDK types from consumers.
/// </summary>
public interface IBlobContainerClient
{
    /// <summary>Container name for logging and diagnostics.</summary>
    string ContainerName { get; }

    /// <summary>Creates the container when it does not already exist.</summary>
    Task EnsureExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>Uploads a blob and returns its absolute URI.</summary>
    Task<string> UploadAsync(
        Stream content,
        string blobName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Opens a read stream for the blob.</summary>
    Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>Deletes the blob when present.</summary>
    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>Builds the blob URI without transferring content.</summary>
    string GetBlobUrl(string blobName);
}
