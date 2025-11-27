using PhotoAlbumCreator.AlbumLibraries;
using PhotoAlbumCreator.PhotoAlbums.Photos;
using PhotoAlbumCreator.PhotoAlbums.Videos;
using System;
using System.Collections.Generic;
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
    private readonly IReadOnlyList<MediaFile> _mediaFiles;
    private readonly IReadOnlyList<PhotoAlbum> _childAlbums;

    public string Name { get; private set; }
    public string RelativePath { get; private set; }
    public string FullPath { get; private set; }
    public string FilesDirectoryPath { get; private set; }
    public string ReadmePath { get; private set; }
    public string IndexHtmlPath { get; private set; }

    public AlbumLibrary Library => _albumLibrary;

    public IReadOnlyList<MediaFile> MediaFiles => _mediaFiles;
    public IReadOnlyList<PhotoAlbum> ChildAlbums => _childAlbums;

    public PhotoAlbum(
        AlbumLibrary albumLibrary,
        string name,
        IReadOnlyList<MediaFile> mediaFiles,
        IReadOnlyList<PhotoAlbum> albums)
    {
        ArgumentNullException.ThrowIfNull(albumLibrary);
        ArgumentException.ThrowIfNullOrEmpty(name);

        _albumLibrary = albumLibrary;

        name = ProcessName(name);

        Name = Path.GetFileName(name);
        RelativePath = name;
        FullPath = GetFullPath(albumLibrary, name);
        FilesDirectoryPath = GetFilesDirectoryPath(albumLibrary, name);
        ReadmePath = Path.Combine(FilesDirectoryPath, ReadmeFileName);
        IndexHtmlPath = Path.Combine(FullPath, IndexHtmlFileName);

        _mediaFiles = mediaFiles ?? Array.Empty<MediaFile>();
        _childAlbums = albums ?? Array.Empty<PhotoAlbum>();
    }

    public static string GetFilesDirectoryPath(AlbumLibrary albumLibrary, string name)
    {
        return Path.Combine(
            GetFullPath(albumLibrary, name),
            FilesDirectoryName);
    }

    public static string GetFullPath(AlbumLibrary albumLibrary, string name)
    {
        return Path.Combine(albumLibrary.RootPath, ProcessName(name));
    }

    public static string ProcessName(string name)
    {
        return name.Trim();
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
