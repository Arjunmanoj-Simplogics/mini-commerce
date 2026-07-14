namespace MiniCommerce.Storage.Abstractions;

/// <summary>
/// Application-facing blob storage contract. Controllers and services depend on this
/// interface only — never on the Azure SDK.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads content to blob storage and returns the public blob URL (stored in SQL, not bytes).
    /// </summary>
    /// <param name="content">Readable stream of file content.</param>
    /// <param name="blobName">Blob path within the configured container.</param>
    /// <param name="contentType">HTTP content type for the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Absolute blob URI string.</returns>
    Task<string> UploadAsync(
        Stream content,
        string blobName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads blob content as a stream. Caller is responsible for disposing the stream.
    /// </summary>
    Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob if it exists.
    /// </summary>
    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the absolute URI for a blob without downloading its content.
    /// </summary>
    string GetBlobUrl(string blobName);

    /// <summary>
    /// Ensures the configured container exists (used by health checks and first upload).
    /// </summary>
    Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default);
}
