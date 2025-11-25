using System;

namespace PhotoAlbumCreator.AlbumLibraries.Requests;

public sealed class CreateAlbumLibraryRequest
{
    public string RootPath { get; private set; }
    public bool IsForce { get; private set; }

    public CreateAlbumLibraryRequest(string rootPath, bool isForce)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        RootPath = rootPath;
        IsForce = isForce;
    }
}
