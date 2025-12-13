using System.Runtime.CompilerServices;
using Amazon.S3;
using Amazon.S3.Model;

namespace ConsoleApp.S3;

public static class S3ClientExtensions
{
    extension(IAmazonS3 s3Client)
    {
        public async IAsyncEnumerable<List<KeyVersion>> PurgeBucketAsync(string bucketName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string? continuationToken = null;

            do
            {
                var listResponse = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    ContinuationToken = continuationToken
                }, cancellationToken);

                var s3Objects = listResponse.S3Objects ?? [];

                if (s3Objects.Count > 0)
                {
                    var objectsToDelete = s3Objects.Select(obj => new KeyVersion
                    {
                        Key = obj.Key
                    }).ToList();
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = bucketName,
                        Objects = objectsToDelete
                    };

                    await s3Client.DeleteObjectsAsync(deleteRequest, cancellationToken);
                    yield return objectsToDelete;
                }

                continuationToken = listResponse.IsTruncated ?? false 
                    ? listResponse.NextContinuationToken 
                    : null;

            } while (continuationToken != null);
        }
    }
}