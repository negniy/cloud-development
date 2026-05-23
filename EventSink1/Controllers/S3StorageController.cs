using EventSink1.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json.Nodes;

namespace EventSink1.Controllers;

[ApiController]
[Route("api/s3")]
public class S3StorageController(IS3Service s3Service, ILogger<S3StorageController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        logger.LogInformation("Получен запрос списка файлов");
        try
        {
            var list = await s3Service.GetFileList();
            logger.LogInformation("Найдено файлов: {count}", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка файлов");
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        logger.LogInformation("Запрошен файл: {key}", key);
        try
        {
            var node = await s3Service.DownloadFile(key);
            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при скачивании файла {key}", key);
            return BadRequest(ex.Message);
        }
    }
}