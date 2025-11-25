using PhotoAlbumCreator.AlbumLibraries.Requests;
using System;

namespace PhotoAlbumCreator.PhotoAlbums.Requests;

public sealed class CreatePhotoAlbumRequest
{
    public string RootPath { get; private set; }
    public string Name { get; private set; }

    public CreatePhotoAlbumRequest(string rootPath, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        RootPath = rootPath;
        Name = name;
    }

    public CreateAlbumLibraryRequest ToCreateAlbumLibrary()
    {
        return new CreateAlbumLibraryRequest(RootPath, false);
    }
}
