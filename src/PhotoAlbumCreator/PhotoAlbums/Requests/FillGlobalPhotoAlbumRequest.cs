using PhotoAlbumCreator.AlbumLibraries.Requests;
using System;

namespace PhotoAlbumCreator.PhotoAlbums.Requests;

public sealed class FillGlobalPhotoAlbumRequest
{
    public string RootPath { get; private set; }

    public FillGlobalPhotoAlbumRequest(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        RootPath = rootPath;
    }

    public CreateAlbumLibraryRequest ToCreateAlbumLibrary()
    {
        return new CreateAlbumLibraryRequest(RootPath, false);
    }
}
