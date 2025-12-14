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
}