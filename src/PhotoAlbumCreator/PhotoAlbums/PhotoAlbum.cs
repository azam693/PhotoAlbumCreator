using PhotoAlbumCreator.AlbumLibraries;
using PhotoAlbumCreator.PhotoAlbums.Photos;
using PhotoAlbumCreator.PhotoAlbums.Videos;
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
    
    public bool IsRoot { get; private set; }
    public string Name { get; private set; }
    public string RelativePath { get; private set; }
    public string FullPath { get; private set; }
    public string FilesDirectoryPath { get; private set; }
    public string ReadmePath { get; private set; }
    public string IndexHtmlPath { get; private set; }

    public AlbumLibrary Library => _albumLibrary;

    public PhotoAlbum(AlbumLibrary albumLibrary, )
        : this(albumLibrary, albumLibrary.RootPath)
    {
        
    }

    public PhotoAlbum(AlbumLibrary albumLibrary, string name)
    {
        ArgumentNullException.ThrowIfNull(albumLibrary);
        ArgumentException.ThrowIfNullOrEmpty(name);

        _albumLibrary = albumLibrary;

        name = name.Trim();

        IsRoot = albumLibrary.RootPath.Equals(name, StringComparison.InvariantCultureIgnoreCase);
        Name = Path.GetFileName(name);
        RelativePath = name;
        FullPath = Path.Combine(albumLibrary.RootPath, name.Trim());
        FilesDirectoryPath = Path.Combine(FullPath, FilesDirectoryName);
        ReadmePath = Path.Combine(FilesDirectoryPath, ReadmeFileName);
        IndexHtmlPath = Path.Combine(FullPath, IndexHtmlFileName);
    }

    public static bool IsSupportedFile(FileInfo fileInfo)
    {
        return IsSupportedExtension(fileInfo.Extension);
    }

    public static bool IsSupportedFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return IsSupportedExtension(Path.GetExtension(filePath));
    }

    public static bool IsSupportedExtension(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        extension = extension.ToLowerInvariant();

        return Photo.IsSupportedExtension(extension)
            || Video.IsSupportedExtension(extension);
    }

}
