using AwsDocumentPortal.Models;
namespace AwsDocumentPortal.Services;
public interface IDocumentStorage{Task<DocumentItem>SaveAsync(IFormFile file,string documentType,CancellationToken cancellationToken);IReadOnlyCollection<DocumentItem>GetAll();}