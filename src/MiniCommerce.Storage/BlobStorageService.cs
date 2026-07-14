using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniCommerce.Storage.Abstractions;
using MiniCommerce.Storage.Internal;
using MiniCommerce.Storage.Options;

namespace MiniCommerce.Storage;

/// <summary>
/// Production implementation of <see cref="IBlobStorageService"/> backed by Azure Blob Storage.
/// Uses <see cref="IOptions{BlobStorageOptions}"/> for configuration and delegates SDK access
/// to <see cref="IBlobContainerClientFactory"/> for testability.
/// </summary>
public sealed class BlobStorageService : IBlobStorageService
{
    private readonly IBlobContainerClient _container;
    private readonly ILogger<BlobStorageService> _logger;

    /// <summary>
    /// Initializes blob storage from options and a factory (injected for testing).
    /// </summary>
    public BlobStorageService(
        IOptions<BlobStorageOptions> options,
        IBlobContainerClientFactory containerClientFactory,
        ILogger<BlobStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(containerClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        var settings = options.Value;
        if (!settings.Enabled)
        {
            throw new InvalidOperationException(
                "BlobStorageService was resolved but BlobStorage:Enabled is false. " +
                "Register IBlobStorageService only when blob storage is enabled.");
        }

        _logger = logger;
        _container = containerClientFactory.Create(settings);
    }

    /// <inheritdoc />
    public Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default)
        => _container.EnsureExistsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<string> UploadAsync(
        Stream content,
        string blobName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        using var activity = StartStorageActivity("Blob.Upload", blobName);
        try
        {
            await _container.EnsureExistsAsync(cancellationToken);
            var blobUrl = await _container.UploadAsync(content, blobName, contentType, cancellationToken);

            _logger.LogInformation(
                "Uploaded blob {BlobName} to container {ContainerName}. Url={BlobUrl} TraceId={TraceId} SpanId={SpanId}",
                blobName,
                _container.ContainerName,
                blobUrl,
                activity?.TraceId.ToString(),
                activity?.SpanId.ToString());

            return blobUrl;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        using var activity = StartStorageActivity("Blob.Download", blobName);
        try
        {
            _logger.LogDebug(
                "Downloading blob {BlobName} from container {ContainerName} TraceId={TraceId} SpanId={SpanId}",
                blobName,
                _container.ContainerName,
                activity?.TraceId.ToString(),
                activity?.SpanId.ToString());
            return await _container.DownloadAsync(blobName, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        using var activity = StartStorageActivity("Blob.Delete", blobName);
        try
        {
            await _container.DeleteAsync(blobName, cancellationToken);

            _logger.LogInformation(
                "Deleted blob {BlobName} from container {ContainerName} TraceId={TraceId} SpanId={SpanId}",
                blobName,
                _container.ContainerName,
                activity?.TraceId.ToString(),
                activity?.SpanId.ToString());
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public string GetBlobUrl(string blobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        return _container.GetBlobUrl(blobName);
    }

    private Activity? StartStorageActivity(string name, string blobName)
    {
        var activity = StorageTelemetry.ActivitySource.StartActivity(name, ActivityKind.Client);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("storage.system", "azure_blob");
        activity.SetTag("storage.container", _container.ContainerName);
        activity.SetTag("storage.blob_name", blobName);
        return activity;
    }
}
