using SkiaSharp;
using ExifInterface = AndroidX.ExifInterface.Media.ExifInterface;

namespace Vibik.Utils.Compress;

public static partial class CompressionUtils
{
    private static partial byte[] CompressToJpegPlatform(byte[] originalBytes)
    {
        const int maxWidth = 1920;
        const int maxHeight = 1080;
        const int quality = 80;

        using var bitmap = DecodeWithOrientation(originalBytes);

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

    private static SKBitmap DecodeWithOrientation(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);

        var exif = new ExifInterface(ms);
        var orientation = exif.GetAttributeInt(ExifInterface.TagOrientation, ExifInterface.OrientationNormal);

        ms.Position = 0;
        using var src = SKBitmap.Decode(ms) ?? throw new InvalidOperationException("Не удалось декодировать изображение");

        return orientation switch
        {
            ExifInterface.OrientationRotate90 => Rotate(src, 90),
            ExifInterface.OrientationRotate180 => Rotate(src, 180),
            ExifInterface.OrientationRotate270 => Rotate(src, 270),

            ExifInterface.OrientationFlipHorizontal => Flip(src, horizontal: true),
            ExifInterface.OrientationFlipVertical => Flip(src, horizontal: false),

            ExifInterface.OrientationTranspose => Transpose(src),
            ExifInterface.OrientationTransverse => Transverse(src),

            _ => src.Copy()
        };
    }

    private static SKBitmap Rotate(SKBitmap src, int degrees)
    {
        var isVertical = degrees is 90 or 270;
        var w = isVertical ? src.Height : src.Width;
        var h = isVertical ? src.Width : src.Height;

        var dst = new SKBitmap(w, h);
        using var canvas = new SKCanvas(dst);

        canvas.Translate(w / 2f, h / 2f);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-src.Width / 2f, -src.Height / 2f);
        canvas.DrawBitmap(src, 0, 0);

        return dst;
    }

    private static SKBitmap Flip(SKBitmap src, bool horizontal)
    {
        var dst = new SKBitmap(src.Width, src.Height);
        using var canvas = new SKCanvas(dst);

        if (horizontal)
        {
            canvas.Scale(-1, 1);
            canvas.Translate(-src.Width, 0);
        }
        else
        {
            canvas.Scale(1, -1);
            canvas.Translate(0, -src.Height);
        }

        canvas.DrawBitmap(src, 0, 0);
        return dst;
    }

    private static SKBitmap Transpose(SKBitmap src)
    {
        var dst = new SKBitmap(src.Height, src.Width);
        using var canvas = new SKCanvas(dst);

        canvas.Translate(dst.Width / 2f, dst.Height / 2f);
        canvas.RotateDegrees(90);
        canvas.Scale(1, -1);
        canvas.Translate(-src.Width / 2f, -src.Height / 2f);
        canvas.DrawBitmap(src, 0, 0);

        return dst;
    }

    private static SKBitmap Transverse(SKBitmap src)
    {
        var dst = new SKBitmap(src.Height, src.Width);
        using var canvas = new SKCanvas(dst);

        canvas.Translate(dst.Width / 2f, dst.Height / 2f);
        canvas.RotateDegrees(90);
        canvas.Scale(-1, 1);
        canvas.Translate(-src.Width / 2f, -src.Height / 2f);
        canvas.DrawBitmap(src, 0, 0);

        return dst;
    }
}
