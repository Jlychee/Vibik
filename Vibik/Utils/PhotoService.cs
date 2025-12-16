using Core.Domain;

namespace Vibik.Utils;

using Microsoft.Maui.Devices;

public static class PhotoService
{
    public static async Task<FileResult?> PickOrCaptureAsync(Page page)
    {
        var action = await page.DisplayActionSheet("Добавить фото", "Отмена", null, "Сделать фото", "Выбрать из галереи");
        if (string.IsNullOrEmpty(action) || action == "Отмена") return null;


        if (action == "Сделать фото")
        {
            var cameraGranted = await EnsurePermission<Permissions.Camera>();
            var mediaGranted = await EnsureMediaAccessAsync();
            if (!cameraGranted || !mediaGranted)
            {
                await page.DisplayAlert("Нет доступа", "Разрешите доступ к камере и фото/медиа.", "OK");
                return null;
            }
            return await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg"
            });
        }
        if (!await EnsureMediaAccessAsync())
        {
            await page.DisplayAlert("Нет доступа", "Разрешите доступ к фото/медиа.", "OK");
            return null;
        }
        return await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Выберите фото" });
    }
    
    private static Task<bool> EnsureMediaAccessAsync()
    {
#if ANDROID
        return DeviceInfo.Version.Major >= 13
            ? EnsurePermission<Permissions.Photos>()
            : EnsureLegacyStorageAsync();
#else
        return EnsurePermission<Permissions.Photos>();
#endif
    }

#if ANDROID
    private static async Task<bool> EnsureLegacyStorageAsync()
    {
        var readGranted = await EnsurePermission<Permissions.StorageRead>();
        var writeGranted = await EnsurePermission<Permissions.StorageWrite>();
        return readGranted && writeGranted;
    }
#endif


    private static async Task<bool> EnsurePermission<T>() where T : Permissions.BasePermission, new()
    {
        var status = await Permissions.CheckStatusAsync<T>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<T>();
        return status == PermissionStatus.Granted;
    }
    
    public static async Task<string> SaveFileResultAsync(FileResult file, string taskKey)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

        var dir = GetTaskDirectory(taskKey);
        Directory.CreateDirectory(dir);

        var name = $"photo_{DateTime.Now:yyyyMMdd_HHmmssfff}{ext}";
        var dst = Path.Combine(dir, name);

        await using var srcStream = await file.OpenReadAsync();
        await using var dstStream = File.Create(dst);
        await srcStream.CopyToAsync(dstStream);

        return dst;
    }
    
    public static async Task<string> SavePickedFileToCacheAsync(FileResult file)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

        var localPath = Path.Combine(FileSystem.CacheDirectory, $"picked_{Guid.NewGuid():N}{ext}");

        await using var src = await file.OpenReadAsync();
        await using var dst = File.Open(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await src.CopyToAsync(dst);

        return localPath;
    }

    private static string GetTaskDirectory(string taskKey) =>
        Path.Combine(FileSystem.AppDataDirectory, "tasks", taskKey);
    
    public static void LoadLocalPhotos(TaskModel task, string taskKey)
    {
        var extendedInfo = task.ExtendedInfo;
        var dir = GetTaskDirectory(taskKey);

        var keep = new List<Uri>(extendedInfo.UserPhotos.Count);
        var existingLocal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var uri in extendedInfo.UserPhotos)
        {
            var path = ImageSourceFinder.GetPathFromUri(uri);

            if (string.IsNullOrWhiteSpace(path) || !ImageSourceFinder.IsLocalPath(path))
            {
                keep.Add(uri);
                continue;
            }

            if (File.Exists(path))
            {
                keep.Add(uri);
                existingLocal.Add(path);
            }
        }

        extendedInfo.UserPhotos.Clear();
        extendedInfo.UserPhotos.AddRange(keep);

        if (!Directory.Exists(dir))
            return;

        foreach (var path in Directory.EnumerateFiles(dir).OrderBy(x => x))
        {
            if (!File.Exists(path)) continue;

            if (existingLocal.Add(path))
                extendedInfo.UserPhotos.Add(new Uri(path));
        }
    }
    
    public static void DeleteAllTaskLocalPhotosExcept(string taskKey)
    {
        var dir = GetTaskDirectory(taskKey);
        if (!Directory.Exists(dir)) return;
        
        foreach (var file in Directory.EnumerateFiles(dir))
        {
            try
            {
                var full = Path.GetFullPath(file); 
                File.Delete(full);
            }
            catch { }
        }
    }

}

    
