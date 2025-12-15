using SkiaSharp;

namespace Vibik.Utils;

public static class CompressionUtils
{
    public static byte[] CompressToJpeg(byte[] originalBytes)
    {
        const int maxWidth = 1920;
        const int maxHeight = 1080;
        const int quality = 80;

        using var bitmap = SKBitmap.Decode(originalBytes);
        if (bitmap == null)
            throw new InvalidOperationException("Не удалось декодировать изображение");

        var width = bitmap.Width;
        var height = bitmap.Height;

        if (width > maxWidth || height > maxHeight)
        {
            var scale = Math.Min((float)maxWidth / width, (float)maxHeight / height);
            width = (int)(width * scale);
            height = (int)(height * scale);
        }

        using var resized = bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium);
        using var image = SKImage.FromBitmap(resized ?? bitmap);

        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }
    
    public static async Task JpegToPath(List<string> localPaths, List<string> tempCompressedPaths)
    {
        foreach (var src in localPaths)
        {
            var originalBytes = await File.ReadAllBytesAsync(src);
            var jpegBytes = CompressionUtils.CompressToJpeg(originalBytes);

            var tempPath = Path.Combine(FileSystem.CacheDirectory, $"vibik_{Guid.NewGuid():N}.jpg");
            await File.WriteAllBytesAsync(tempPath, jpegBytes);

            tempCompressedPaths.Add(tempPath);
        }
    }
}