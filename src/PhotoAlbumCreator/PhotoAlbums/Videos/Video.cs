using System.Collections.Generic;
using System.Linq;

namespace PhotoAlbumCreator.PhotoAlbums.Videos;

public sealed class Video
{
    public static readonly IReadOnlyList<string> Extensions = [
        ".mp4", ".webm", ".mov", ".m4v", ".avi", ".mkv"
    ];

    public static bool IsSupportedExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;

        return Extensions.Contains(extension.ToLowerInvariant());
    }
}
