using PhotoAlbumCreator.AlbumLibraries.Requests;
using System;

namespace PhotoAlbumCreator.PhotoAlbums.Requests;

public sealed class FillGlobalPhotoAlbumRequest
{
    public string RootPath { get; private set; }
    public OrderAlbumFields OrderAlbumField { get; private set; }

    public FillGlobalPhotoAlbumRequest(string rootPath, OrderAlbumFields orderAlbumField)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        RootPath = rootPath;
        OrderAlbumField = orderAlbumField;
    }

    public CreateAlbumLibraryRequest ToCreateAlbumLibrary()
    {
        return new CreateAlbumLibraryRequest(RootPath, false);
    }
}
