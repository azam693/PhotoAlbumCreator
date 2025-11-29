using System;

namespace PhotoAlbumCreator.PhotoAlbums.Requests;

public sealed class FillPhotoAlbumRequest
{
    public string RootPath { get; private set; }
    public string Name { get; private set; }
    public OrderAlbumFields OrderAlbumField { get; private set; }

    public FillPhotoAlbumRequest(string rootPath, string name, OrderAlbumFields orderAlbumField)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        RootPath = rootPath;
        Name = name;
        OrderAlbumField = orderAlbumField;
    }
    
    public CreatePhotoAlbumRequest ToCreatePhotoAlbum()
    {
        return new CreatePhotoAlbumRequest(RootPath, Name);
    }
}
