using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MiniCommerce.AzureAuth;
using MiniCommerce.Storage.Abstractions;
using MiniCommerce.Storage.Options;

namespace MiniCommerce.Storage.Internal;

/// <summary>
/// Azure SDK adapter for <see cref="IBlobContainerClient"/>. Not exposed outside this assembly.
/// </summary>
internal sealed class AzureBlobContainerClient : IBlobContainerClient
{
    private readonly BlobContainerClient _container;

    public AzureBlobContainerClient(BlobContainerClient container)
    {
        _container = container;
        ContainerName = container.Name;
    }

    public string ContainerName { get; }

    public async Task EnsureExistsAsync(CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string blobName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var blob = _container.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            },
            cancellationToken);

        return blob.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var blob = _container.GetBlobClient(blobName);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        await _container.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
    }

    public string GetBlobUrl(string blobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        return _container.GetBlobClient(blobName).Uri.ToString();
    }
}

/// <summary>
/// Creates Azure blob container clients.
/// Development: connection string. Production / MI: ServiceUri + shared <see cref="IAzureCredentialProvider"/>.
/// </summary>
internal sealed class AzureBlobContainerClientFactory : IBlobContainerClientFactory
{
    private readonly IAzureCredentialProvider _credentialProvider;

    public AzureBlobContainerClientFactory(IAzureCredentialProvider credentialProvider)
    {
        _credentialProvider = credentialProvider;
    }

    public IBlobContainerClient Create(BlobStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ContainerName);

        var clientOptions = BuildClientOptions(options);
        BlobServiceClient serviceClient;

        // Development: connection string. Production: Managed Identity via DefaultAzureCredential.
        var preferMi = _credentialProvider.PreferManagedIdentity;
        if (!preferMi && !string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            serviceClient = new BlobServiceClient(options.ConnectionString, clientOptions);
        }
        else
        {
            var serviceUri = ResolveServiceUri(options);
            serviceClient = new BlobServiceClient(serviceUri, _credentialProvider.Credential, clientOptions);
        }

        return new AzureBlobContainerClient(serviceClient.GetBlobContainerClient(options.ContainerName));
    }

    private static BlobClientOptions BuildClientOptions(BlobStorageOptions options)
    {
        var clientOptions = new BlobClientOptions();
        clientOptions.Retry.Mode = RetryMode.Exponential;
        clientOptions.Retry.MaxRetries = Math.Max(0, options.MaxRetryCount);
        clientOptions.Retry.Delay = TimeSpan.FromSeconds(Math.Max(0, options.RetryDelaySeconds));
        clientOptions.Retry.MaxDelay = TimeSpan.FromSeconds(Math.Max(1, options.MaxRetryDelaySeconds));
        clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(Math.Max(1, options.NetworkTimeoutSeconds));
        return clientOptions;
    }

    private static Uri ResolveServiceUri(BlobStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ServiceUri))
        {
            return new Uri(options.ServiceUri);
        }

        if (!string.IsNullOrWhiteSpace(options.AccountName))
        {
            return new Uri($"https://{options.AccountName}.blob.core.windows.net");
        }

        throw new InvalidOperationException(
            "BlobStorage requires ConnectionString (Development) or ServiceUri/AccountName (Production Managed Identity).");
    }
}
