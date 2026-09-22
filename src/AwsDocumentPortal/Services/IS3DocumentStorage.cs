using AwsDocumentPortal.Models;

namespace AwsDocumentPortal.Services;

public interface IS3DocumentStorage
{
    Task<DocumentItem> UploadAsync(
        IFormFile file,
        string documentType,
        CancellationToken cancellationToken);
}