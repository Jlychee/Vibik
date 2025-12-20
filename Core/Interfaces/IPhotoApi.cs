namespace Core.Interfaces;

public interface IPhotoApi
{
    Task<string?> UploadAsync(string filePath, CancellationToken ct = default);
}
