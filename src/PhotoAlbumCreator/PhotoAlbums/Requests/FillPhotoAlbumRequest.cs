using System;

namespace PhotoAlbumCreator.PhotoAlbums.Requests;

public sealed class FillPhotoAlbumRequest
{
    public string RootPath { get; private set; }
    public string Name { get; private set; }

    public FillPhotoAlbumRequest(string rootPath, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        RootPath = rootPath;
        Name = name;
    }
    
    public CreatePhotoAlbumRequest ToCreatePhotoAlbum()
    {
        return new CreatePhotoAlbumRequest(RootPath, Name);
    }
}
