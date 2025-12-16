namespace Vibik.Utils;

public static partial class CompressionUtils
{
    public static byte[] CompressToJpeg(byte[] originalBytes)
        => CompressToJpegPlatform(originalBytes);

    private static partial byte[] CompressToJpegPlatform(byte[] originalBytes);
    
    public static async Task<List<string>> JpegToPaths(List<string> localPaths)
    {
        var result = new List<string>(localPaths.Count);

        foreach (var src in localPaths)
        {
            var originalBytes = await File.ReadAllBytesAsync(src);
            var jpegBytes = CompressToJpeg(originalBytes);

            var tempPath = Path.Combine(FileSystem.CacheDirectory, $"vibik_{Guid.NewGuid():N}.jpg");
            await File.WriteAllBytesAsync(tempPath, jpegBytes);

            result.Add(tempPath);
        }

        return result;
    }

}