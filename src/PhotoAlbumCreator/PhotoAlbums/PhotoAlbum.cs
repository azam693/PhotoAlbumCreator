using PhotoAlbumCreator.AlbumLibraries;
using System;
using System.IO;

namespace PhotoAlbumCreator.PhotoAlbums;

public sealed class PhotoAlbum
{
    public const string FilesDirectoryName = "Files";

    public const string ReadmeFileName = "README.md";
    public const string ReadmeFileTemplateName = "README_Files.md";
    public const string IndexHtmlFileName = "index.html";
    public const string IndexHtmlTemplateName = "index_template.html";

    private readonly AlbumLibrary _albumLibrary;
    
    public string Name { get; private set; }
    public string FullPath { get; private set; }
    public string FilesDirectoryPath { get; private set; }
    public string ReadmePath { get; private set; }
    public string IndexHtmlPath { get; private set; }

    public AlbumLibrary Library => _albumLibrary;

    public PhotoAlbum(AlbumLibrary albumLibrary, string name)
    {
        ArgumentNullException.ThrowIfNull(albumLibrary);
        ArgumentException.ThrowIfNullOrEmpty(name);

        _albumLibrary = albumLibrary;

        Name = name.Trim();
        FullPath = Path.Combine(albumLibrary.RootPath, Name);
        FilesDirectoryPath = Path.Combine(FullPath, FilesDirectoryName);
        ReadmePath = Path.Combine(FilesDirectoryPath, ReadmeFileName);
        IndexHtmlPath = Path.Combine(FullPath, IndexHtmlFileName);
    }
}
