using SkiaSharp;

namespace Vibik.Utils;

public static class CompressionUtils
{
    public static byte[] CompressToJpeg(byte[] originalBytes)
    {
        const int maxWidth = 1920;
        const int maxHeight = 1080;
        const int quality = 80;

        // 1. Декодируем изображение из байт
        using var bitmap = SKBitmap.Decode(originalBytes);
        if (bitmap == null)
            throw new InvalidOperationException("Не удалось декодировать изображение");

        int width = bitmap.Width;
        int height = bitmap.Height;

        // 2. Масштабируем, если оно превышает нужные размеры
        if (width > maxWidth || height > maxHeight)
        {
            float scale = Math.Min((float)maxWidth / width, (float)maxHeight / height);
            width = (int)(width * scale);
            height = (int)(height * scale);
        }

        using var resized = bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium);
        using var image = SKImage.FromBitmap(resized ?? bitmap);

        // 3. Сохраняем в JPEG с нужным качеством
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }
}