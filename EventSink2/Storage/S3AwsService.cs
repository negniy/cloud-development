using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using System.Text.Json.Nodes;

namespace EventSink2.Storage;

public class S3AwsService(IAmazonS3 s3Client, ILogger<S3AwsService> logger) : IS3Service
{
    private const string BucketName = "landplot-bucket";

    public async Task EnsureBucketExists()
    {
        try
        {
            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, BucketName);
            if (!exists)
            {
                await s3Client.PutBucketAsync(BucketName);
                logger.LogInformation("Bucket {Bucket} успешно создан", BucketName);
            }
            else
            {
                logger.LogInformation("Bucket {Bucket} уже существует", BucketName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при проверке/создании bucket {Bucket}", BucketName);
        }
    }

    public async Task UploadFile(string jsonContent)
    {
        var key = $"landplot_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.json";

        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            ContentBody = jsonContent,
            ContentType = "application/json"
        };

        await s3Client.PutObjectAsync(request);
        logger.LogInformation("Файл загружен в S3: {Key}", key);
    }

    public async Task<List<string>> GetFileList()
    {
        var response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = BucketName
        });

        return response.S3Objects.Select(o => o.Key).ToList();
    }

    public async Task<JsonNode> DownloadFile(string key)
    {
        var response = await s3Client.GetObjectAsync(BucketName, key);
        using var reader = new StreamReader(response.ResponseStream);
        var content = await reader.ReadToEndAsync();
        return JsonNode.Parse(content)!;
    }
}