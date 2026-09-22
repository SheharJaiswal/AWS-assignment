using Amazon.S3;
using Amazon.S3.Model;
using AwsDocumentPortal.Models;

namespace AwsDocumentPortal.Services;

public sealed class S3DocumentStorage(
    IAmazonS3 s3,
    IConfiguration configuration) : IS3DocumentStorage
{
    public async Task<DocumentItem> UploadAsync(
        IFormFile file,
        string documentType,
        CancellationToken cancellationToken)
    {
        var bucketName = configuration["DocumentStorage:BucketName"];

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("S3 bucket is not configured.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var id = Guid.NewGuid();
        var prefix = configuration["DocumentStorage:Prefix"]?.Trim('/');
        var storageKey = string.IsNullOrWhiteSpace(prefix)
            ? $"{id:N}{extension}"
            : $"{prefix}/{id:N}{extension}";

        await using var stream = file.OpenReadStream();

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = storageKey,
            InputStream = stream,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await s3.PutObjectAsync(request, cancellationToken);

        return new DocumentItem
        {
            Id = id,
            FileName = Path.GetFileName(file.FileName),
            DocumentType = documentType,
            SizeBytes = file.Length,
            StorageKey = storageKey,
            UploadedAtUtc = DateTimeOffset.UtcNow
        };
    }
}