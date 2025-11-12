using System.Collections.Generic;
using System.Linq;

namespace PhotoAlbumCreator.PhotoAlbums.Photos;

public sealed class Photo
{
    public static readonly IReadOnlyList<string> Extensions = [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff"
    ];

    public static bool IsSupportedExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        return Extensions.Contains(extension.ToLowerInvariant());
    }
}
