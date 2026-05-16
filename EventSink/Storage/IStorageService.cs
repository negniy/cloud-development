namespace PatientApp.EventSink.Storage;

public interface IStorageService
{
    public Task SaveAsync(string fileName, string content);
}