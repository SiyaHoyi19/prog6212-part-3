using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace RaceDay.Api.Services;

public record BlobImage(Stream Content, string ContentType);

public class BlobStorageService
{
    private static readonly string[] AllowedTypes =
        { "image/jpeg", "image/png", "image/webp" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration configuration)
    {
        string cs = configuration["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Blob connection string missing.");
        string name = configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        _container = new BlobContainerClient(cs, name);
    }

    public static bool IsValidImage(IFormFile? file) =>
        file is { Length: > 0 }
        && file.Length <= MaxBytes
        && AllowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase);

    public async Task<string> UploadAsync(
        IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(
            PublicAccessType.Blob, cancellationToken: cancellationToken);

        string blobName = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        BlobClient blob = _container.GetBlobClient(blobName);

        await using Stream stream = file.OpenReadStream();
        await blob.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = file.ContentType },
            cancellationToken: cancellationToken);

        return blob.Uri.ToString();
    }

    public async Task<BlobImage?> DownloadAsync(
        string blobUrl, CancellationToken cancellationToken = default)
    {
        string blobName = new BlobUriBuilder(new Uri(blobUrl)).BlobName;
        BlobClient blob = _container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(cancellationToken))
            return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return new BlobImage(download.Value.Content, download.Value.Details.ContentType);
    }
}
