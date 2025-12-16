using Core.Domain;

namespace Vibik.Utils;

public static class ImageSourceFinder
{
    public static ImageSource ResolveImage(string? pathOrUrl, string fallback = "example_collage.png")
    { 
        if (string.IsNullOrWhiteSpace(pathOrUrl))
            return ImageSource.FromFile(fallback);

        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                return ImageSource.FromUri(uri);

            if (uri.IsFile && File.Exists(uri.LocalPath))
                return ImageSource.FromFile(uri.LocalPath);
        }

        return ImageSource.FromFile(File.Exists(pathOrUrl) ? pathOrUrl : fallback);
    }
    
    public static string GetPathFromUri(Uri uri)
    {
        if (uri == null)
            return string.Empty;

        if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            return uri.OriginalString;

        if (uri.IsFile)
            return uri.LocalPath;

        return !string.IsNullOrEmpty(uri.OriginalString) ? uri.OriginalString : uri.AbsolutePath;
    }
    
    public static bool IsLocalPath(string urlOrPath)
    {
        if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out var uri))
            return !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        return true;
    }
    
    public static List<string> GetUserPhotosPath(TaskModel task, bool needLocal = false, bool needExisting = false) => task.ExtendedInfo.UserPhotos
            .Select(GetPathFromUri)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Where(u => !needLocal || IsLocalPath(u))
            .Where(u => !needExisting || File.Exists(u))
            .ToList();
    
    public static void CleanupTempFiles(IEnumerable<string> paths)
    {
        foreach (var p in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(p)) File.Delete(p);
            }
            catch { }
        }
    }
    
    public static void TryDeleteLocal(string path)
    {
        try
        {
            if (IsLocalPath(path) && File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignored
        }
    }
    
    public static void EnsureOnlyLocalPhotosForEditableStatuses(TaskModel taskModel)
    {
        if (taskModel.ModerationStatus is ModerationStatus.Approved || taskModel.Completed)
            return;

        taskModel.ExtendedInfo.UserPhotos.RemoveAll(u =>
        {
            var p = GetPathFromUri(u);
            return !string.IsNullOrWhiteSpace(p) && !ImageSourceFinder.IsLocalPath(p);
        });
    }

    
}