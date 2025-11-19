using System;

namespace PhotoAlbumCreator.PhotoAlbums;

public sealed class MediaFile
{
    public string Name { get; private set; }
    public string FullPath { get; private set; }
    public bool IsImage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public MediaFile(
        string name,
        string fullPath,
        bool isImage,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);

        Name = name;
        FullPath = fullPath;
        IsImage = isImage;
        CreatedAt = createdAt;
    }

    public string GetCreatedAtISO()
    {
        return CreatedAt.ToString("yyyy-MM-dd");
    }

    public string GetCreatedAtHuman()
    {
        return CreatedAt.ToString("d MMMM yyyy");
    }
}
