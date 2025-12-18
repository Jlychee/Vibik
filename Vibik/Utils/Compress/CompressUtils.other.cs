
#if !ANDROID
using SkiaSharp;

namespace Vibik.Utils.Compress;

public static partial class CompressionUtils
{
    private static partial byte[] CompressToJpegPlatform(byte[] originalBytes)
    {
        const int maxWidth = 1920;
        const int maxHeight = 1080;
        const int quality = 80;

        using var bitmap = SKBitmap.Decode(originalBytes)
            ?? throw new InvalidOperationException("Не удалось декодировать изображение");

        var scale = Math.Min(1f, Math.Min((float)maxWidth / bitmap.Width, (float)maxHeight / bitmap.Height));
        var w = (int)Math.Round(bitmap.Width * scale);
        var h = (int)Math.Round(bitmap.Height * scale);

        SKBitmap? resized = null;
        if (scale < 1f)
            resized = bitmap.Resize(new SKImageInfo(w, h), new SKSamplingOptions(SKCubicResampler.Mitchell));

        var bmpToEncode = resized ?? bitmap;

        using var image = SKImage.FromBitmap(bmpToEncode);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

        resized?.Dispose();
        return data.ToArray();
    }
}
#endif