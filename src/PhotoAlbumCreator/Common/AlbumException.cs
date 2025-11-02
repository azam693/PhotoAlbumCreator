using System;

namespace PhotoAlbumCreator.Common;

public class AlbumException : Exception
{
    public AlbumException(string message)
        : base(message)
    {
    }
}
