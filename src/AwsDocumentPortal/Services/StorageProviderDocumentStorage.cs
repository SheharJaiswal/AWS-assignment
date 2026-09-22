using AwsDocumentPortal.Models;
namespace AwsDocumentPortal.Services;
public sealed class StorageProviderDocumentStorage(ILocalDocumentStorage local,IS3DocumentStorage s3,IConfiguration configuration):IDocumentStorage{
public Task<DocumentItem>SaveAsync(IFormFile file,string documentType,CancellationToken cancellationToken)=>string.Equals(configuration["DocumentStorage:Provider"],"S3",StringComparison.OrdinalIgnoreCase)?s3.UploadAsync(file,documentType,cancellationToken):local.SaveAsync(file,documentType,cancellationToken);
public IReadOnlyCollection<DocumentItem>GetAll()=>local.GetAll();
}