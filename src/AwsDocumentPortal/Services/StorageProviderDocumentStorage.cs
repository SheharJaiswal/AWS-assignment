using AwsDocumentPortal.Models;

namespace AwsDocumentPortal.Services;

public sealed class StorageProviderDocumentStorage(
    ILocalDocumentStorage localStorage,
    IS3DocumentStorage s3Storage,
    IConfiguration configuration) : IDocumentStorage
{
    public Task<DocumentItem> SaveAsync(
        IFormFile file,
        string documentType,
        CancellationToken cancellationToken) =>
        string.Equals(
            configuration["DocumentStorage:Provider"],
            "S3",
            StringComparison.OrdinalIgnoreCase)
            ? s3Storage.UploadAsync(file, documentType, cancellationToken)
            : localStorage.SaveAsync(file, documentType, cancellationToken);

    public IReadOnlyCollection<DocumentItem> GetAll() => localStorage.GetAll();
}