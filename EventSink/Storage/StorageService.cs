using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace PatientApp.EventSink.Storage;

public class StorageService(
    IAmazonS3 s3Client) : IStorageService
{
    private const string BucketName = "patients";

    public async Task SaveAsync(
        string fileName,
        string content)
    {
        var exists =
            await AmazonS3Util
                .DoesS3BucketExistV2Async(
                    s3Client,
                    BucketName);

        if (!exists)
        {
            await s3Client.PutBucketAsync(
                new PutBucketRequest
                {
                    BucketName = BucketName
                });
        }

        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = fileName,
            ContentBody = content
        };

        await s3Client.PutObjectAsync(request);
    }
}