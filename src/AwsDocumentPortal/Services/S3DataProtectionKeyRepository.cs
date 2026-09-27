using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml.Linq;

namespace AwsDocumentPortal.Services
{
    public class S3DataProtectionKeyRepository : IXmlRepository
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;
        private readonly string _prefix;

        public S3DataProtectionKeyRepository(
            IAmazonS3 s3,
            IConfiguration configuration)
        {
            _s3 = s3;

            _bucketName = configuration["DocumentStorage:BucketName"]
                ?? throw new InvalidOperationException(
                    "DocumentStorage:BucketName is not configured.");

            _prefix = configuration["DataProtection:KeyPrefix"]
                ?? "dataprotection-keys/";
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            var elements = new List<XElement>();

            string? continuationToken = null;

            do
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = _prefix,
                    ContinuationToken = continuationToken
                };

                var response = _s3.ListObjectsV2Async(request)
                    .GetAwaiter()
                    .GetResult();

                foreach (var item in response.S3Objects ?? [])
                {
                    var getRequest = new GetObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = item.Key
                    };

                    using var responseObject = _s3.GetObjectAsync(getRequest)
                        .GetAwaiter()
                        .GetResult();

                    using var reader = new StreamReader(responseObject.ResponseStream);

                    var xml = reader.ReadToEnd();

                    if (!string.IsNullOrWhiteSpace(xml))
                    {
                        elements.Add(XElement.Parse(xml));
                    }
                }

                continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;

            } while (continuationToken != null);

            return elements;
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            var keyId = element.Attribute("id")?.Value;

            if (string.IsNullOrWhiteSpace(keyId))
            {
                keyId = Guid.NewGuid().ToString();
            }

            var key = $"{_prefix}key-{keyId}.xml";

            var xml = element.ToString(SaveOptions.DisableFormatting);

            using var stream = new MemoryStream(
                System.Text.Encoding.UTF8.GetBytes(xml));

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = "application/xml",
                ServerSideEncryptionMethod =
                    ServerSideEncryptionMethod.AES256
            };

            _s3.PutObjectAsync(request)
                .GetAwaiter()
                .GetResult();
        }
    }
}