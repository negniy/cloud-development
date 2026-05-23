using System.Text.Json.Nodes;

namespace EventSink2.Storage;

public interface IS3Service
{
    public Task EnsureBucketExists();
    public Task UploadFile(string jsonContent);
    public Task<List<string>> GetFileList();
    public Task<JsonNode> DownloadFile(string key);
}