using Core.Interfaces;

namespace Vibik.Utils;

public static class PhotoServiceExtensions
{
    public static async Task<string?> PickSaveAndUploadAsync(
        this Page page,
        string taskKey,
        IPhotoApi photoApi,
        CancellationToken ct = default)
    {
        var file = await PhotoService.PickOrCaptureAsync(page);
        if (file is null) return null;

        var localPath = await PhotoService.SaveFileResultAsync(file, taskKey);
        return await photoApi.UploadAsync(localPath, ct);
    }
}
    