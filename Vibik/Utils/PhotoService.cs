using System.Text.RegularExpressions;

namespace Utils;

public static class PhotoService
{
    public static async Task<FileResult?> PickOrCaptureAsync(Page page)
    {
        var action = await page.DisplayActionSheet("Добавить фото", "Отмена", null, "Сделать фото", "Выбрать из галереи");
        if (string.IsNullOrEmpty(action) || action == "Отмена") return null;


        if (action == "Сделать фото")
        {
            if (!await EnsurePermission<Permissions.Camera>())
            {
                await page.DisplayAlert("Нет доступа", "Разрешите доступ к камере.", "OK");
                return null;
            }
            return await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg"
            });
        }
        _ = await EnsurePermission<Permissions.Photos>();
        return await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Выберите фото" });
    }
    
    public static string Slug(string s)
    {
        s = s.ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9\-_]+", "_");
        return s.Trim('_', '-');
    }

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

        var dir = Path.Combine(FileSystem.AppDataDirectory, "tasks", taskKey);
        Directory.CreateDirectory(dir);

        var name = $"photo_{DateTime.Now:yyyyMMdd_HHmmssfff}{ext}";
        var dst = Path.Combine(dir, name);

        await using var srcStream = await file.OpenReadAsync();
        await using var dstStream = File.Create(dst);
        await srcStream.CopyToAsync(dstStream);

        return dst;
    }

}

    