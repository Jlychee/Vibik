using Core.Domain;

namespace Vibik.Utils;

public static class TaskPhotosCleaner
{
    public static void CleanupTaskPhotos(TaskModel task)
    {
        if (task.ExtendedInfo?.UserPhotos is null)
            return;

        var uris = task.ExtendedInfo.UserPhotos.ToList();

        var cacheRoot = Path.GetFullPath(FileSystem.CacheDirectory);
        var appRoot = Path.GetFullPath(FileSystem.AppDataDirectory);

        static bool IsInside(string fullPath, string root) =>
            fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);

        foreach (var uri in uris)
        {
            if (!uri.IsFile)
                continue;

            var full = Path.GetFullPath(uri.LocalPath);
            if (!IsInside(full, cacheRoot) && !IsInside(full, appRoot))
                continue;

            try
            {
                if (File.Exists(full))
                    File.Delete(full);
            }
            catch
            {
                //ignored
            }
        }

        task.ExtendedInfo.UserPhotos.Clear();
    }

    public static void DeleteTaskFolder(int userTaskId)
    {
        var dir = Path.Combine(FileSystem.AppDataDirectory, "task_photos", userTaskId.ToString());
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        catch
        {
            //ignored
        }
    }
}