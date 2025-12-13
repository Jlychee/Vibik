using System.Text.RegularExpressions;
using Domain.Models;

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
    
    public static IReadOnlyList<string> GetSavedPhotos(string taskKey)
    {
        var dir = GetTaskDirectory(taskKey);
        if (!Directory.Exists(dir)) return Array.Empty<string>();

        return Directory.GetFiles(dir)
            .OrderBy(f => f)
            .ToArray();
    }

    // private static Task<List<string>> GetLocalPaths(TaskExtendedInfo extendedInfo)
    // {
    //     return Task.FromResult(extendedInfo.UserPhotos
    //         .Select(p => p.Url)
    //         .Where(p => !string.IsNullOrWhiteSpace(p))
    //         .Where(p => IsLocalPath(p))
    //         .Where(File.Exists)
    //         .ToList());
    // }

    private static bool IsLocalPath(string urlOrPath)
    {
        if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out var uri))
            return !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        return true;
    }


    private static string GetTaskDirectory(string taskKey) =>
        Path.Combine(FileSystem.AppDataDirectory, "tasks", taskKey);
}

    