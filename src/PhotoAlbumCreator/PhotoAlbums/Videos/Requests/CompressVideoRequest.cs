using System;

namespace PhotoAlbumCreator.PhotoAlbums.Videos.Requests;

public sealed class CompressVideoRequest
{
    public string Path { get; private set; }

    public CompressVideoRequest(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        Path = path;
    }
}
