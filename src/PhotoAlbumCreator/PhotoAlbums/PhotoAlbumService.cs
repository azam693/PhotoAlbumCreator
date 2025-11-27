using AngleSharp.Io;
using PhotoAlbumCreator.AlbumLibraries;
using PhotoAlbumCreator.Common;
using PhotoAlbumCreator.Common.Settings;
using PhotoAlbumCreator.PhotoAlbums.Htmls;
using PhotoAlbumCreator.PhotoAlbums.Photos;
using PhotoAlbumCreator.PhotoAlbums.Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PhotoAlbumCreator.PhotoAlbums;

public sealed class PhotoAlbumService : AlbumServiceBase
{
    private record AlbumPath(string FullPath, string RelativePath);

    private readonly Resource _resource;
    private readonly AlbumLibraryService _albumLibraryService;

    public PhotoAlbumService(
        Resource resource,
        AppSettingsProvider appSettingsProvider,
        AlbumLibraryService albumLibraryService)
        : base(appSettingsProvider)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(albumLibraryService);

        _resource = resource;
        _albumLibraryService = albumLibraryService;
    }

    public PhotoAlbum Create(CreatePhotoAlbumRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var albumLibrary = _albumLibraryService.Create(request.ToCreateAlbumLibrary());

        Directory.CreateDirectory(PhotoAlbum.GetFullPath(albumLibrary, request.Name));
        Directory.CreateDirectory(PhotoAlbum.GetFilesDirectoryPath(albumLibrary, request.Name));

        var (mediaFiles, photoAlbums) = LoadAlbumResources(
            albumLibrary,
            request.Name,
            maxResourceDeep: 2);
        var photoAlbum = albumLibrary.CreatePhotoAlbum(request.Name, mediaFiles, photoAlbums);

        // Create Readme file
        CreateFile(
            photoAlbum.ReadmePath,
            albumLibrary.GetRelativePath(photoAlbum.ReadmePath),
            _resource.ReadText(PhotoAlbum.ReadmeFileTemplateName));
        // Create Index.html file
        CreateFile(
            photoAlbum.IndexHtmlPath,
            albumLibrary.GetRelativePath(photoAlbum.IndexHtmlPath),
            _resource.ReadText(PhotoAlbum.IndexHtmlTemplateName));

        return photoAlbum;
    }
    
    public void FillGlobal(FillGlobalPhotoAlbumRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var albums = new List<PhotoAlbum>();
        var albumLibrary = _albumLibraryService.Create(request.ToCreateAlbumLibrary());
        var rootAlbum = Create(new CreatePhotoAlbumRequest(
            albumLibrary.RootPath,
            albumLibrary.RootPath));
        var albumStack = new Stack<PhotoAlbum>();
        albumStack.Push(rootAlbum);
        while (albumStack.Any())
        {
            var album = albumStack.Pop();
            var childAlbums = LoadChildPhotoAlbums(
                albumLibrary,
                album.FullPath,
                maxResourceDeep: 1,
                checkIndexHtml: false);
            foreach (var childAlbum in childAlbums)
            {
                albumStack.Push(childAlbum);
            }

            albums.Add(album);
        }

        albums.Reverse();

        foreach (var album in albums)
        {
            Fill(new FillPhotoAlbumRequest(albumLibrary.RootPath, album.RelativePath));
        }
    }

    public void Fill(FillPhotoAlbumRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var photoAlbum = Create(request.ToCreatePhotoAlbum());
        var albumSettings = _appSettingsProvider.LoadAlbumSettings(photoAlbum.Library.SettingsPath);
        var html = File.ReadAllText(photoAlbum.IndexHtmlPath, new UTF8Encoding(false));
        var indexHtmlPage = new IndexHtmlPage(
            html,
            photoAlbum,
            albumSettings.IndexHtml,
            albumSettings.Localization);

        RefreshStyles(indexHtmlPage, photoAlbum);
        RefreshScripts(indexHtmlPage, photoAlbum);

        indexHtmlPage.BuildGallery();

        File.Delete(photoAlbum.IndexHtmlPath);
        File.WriteAllText(photoAlbum.IndexHtmlPath, indexHtmlPage.BuildHtml(), new UTF8Encoding(false));

        Console.WriteLine(
            string.Format(
                _localization.GalleryUpdated,
                photoAlbum.Library.GetRelativePath(photoAlbum.IndexHtmlPath)));
    }

    private void RefreshStyles(IndexHtmlPage indexHtmlPage, PhotoAlbum photoAlbum)
    {
        var stylePaths = indexHtmlPage.GetStyles();
        foreach (var stylePath in stylePaths)
        {
            if (File.Exists(stylePath))
                continue;

            indexHtmlPage.RemoveStyleByPath(stylePath);
        }

        if (!indexHtmlPage.GetStyles().Any())
        {
            var stylePath = GetRelativePath(photoAlbum.Library.StylePath, photoAlbum);
            indexHtmlPage.AddStyle(stylePath);
        }
    }

    private void RefreshScripts(IndexHtmlPage indexHtmlPage, PhotoAlbum photoAlbum)
    {
        var scriptPaths = indexHtmlPage.GetScripts();
        foreach (var scriptPath in scriptPaths)
        {
            if (File.Exists(scriptPath))
                continue;

            indexHtmlPage.RemoveScriptByPath(scriptPath);
        }

        if (!indexHtmlPage.GetScripts().Any())
        {
            var scriptPath = GetRelativePath(photoAlbum.Library.ScriptPath, photoAlbum);
            indexHtmlPage.AddScript(scriptPath);
        }
    }

    private static string GetRelativePath(string resourcePath, PhotoAlbum photoAlbum)
    {
        return Path
            .GetRelativePath(photoAlbum.FullPath, resourcePath)
            .Replace("\\", "/");
    }

    private static (IReadOnlyList<MediaFile> MediaFiles, IReadOnlyList<PhotoAlbum> PhotoAlbums) LoadAlbumResources(
        AlbumLibrary albumLibrary,
        string albumName,
        int maxResourceDeep = 1,
        bool checkFilesPresence = true,
        bool checkIndexHtml = true)
    {
        var mediaFiles = LoadMediaFiles(
            PhotoAlbum.GetFilesDirectoryPath(albumLibrary, albumName));
        var photoAlbums = LoadChildPhotoAlbums(
            albumLibrary,
            PhotoAlbum.GetFullPath(albumLibrary, albumName),
            maxResourceDeep,
            checkFilesPresence,
            checkIndexHtml);

        return (mediaFiles, photoAlbums);
    }

    private static IReadOnlyList<PhotoAlbum> LoadChildPhotoAlbums(
        AlbumLibrary albumLibrary,
        string path,
        int maxResourceDeep = 1,
        bool checkFilesPresence = true,
        bool checkIndexHtml = true)
    {
        maxResourceDeep--;

        return Directory
            .EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly)
            .Select(pathDirectory => albumLibrary.GetRelativePath(pathDirectory))
            .Select(relativePath =>
            {
                var (mediaFiles, photoAlbums) = maxResourceDeep > 0
                    ? LoadAlbumResources(albumLibrary, relativePath, maxResourceDeep: 1)
                    : (Array.Empty<MediaFile>(), Array.Empty<PhotoAlbum>());

                return new PhotoAlbum(albumLibrary, relativePath, mediaFiles, photoAlbums);
            })
            .Where(photoAlbum =>
            {
                if (photoAlbum.Name is PhotoAlbum.FilesDirectoryName
                    or AlbumLibrary.SystemDirectoryName)
                    return false;

                if (checkIndexHtml && !Path.Exists(photoAlbum.IndexHtmlPath))
                    return false;

                if (checkFilesPresence)
                {
                    if (!Path.Exists(photoAlbum.FilesDirectoryPath))
                        return false;

                    return Directory
                        .EnumerateFiles(photoAlbum.FilesDirectoryPath)
                        .Where(filePath => PhotoAlbum.IsSupportedFile(filePath))
                        .Any();
                }
                else
                {
                    return true;
                }
            })
            .ToArray();
    }

    private static IReadOnlyList<MediaFile> LoadMediaFiles(string path)
    {
        var mediaFiles = new List<MediaFile>();
        if (!Path.Exists(path))
            return mediaFiles;

        var directoryInfo = new DirectoryInfo(path);
        foreach (var fileInfo in directoryInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
        {
            if (!PhotoAlbum.IsSupportedFile(fileInfo))
                continue;

            mediaFiles.Add(new MediaFile(
                fileInfo.Name,
                fileInfo.FullName,
                Photo.IsSupportedExtension(fileInfo.Extension.ToLowerInvariant()),
                fileInfo.LastWriteTime));
        }

        return mediaFiles;
    }
}
