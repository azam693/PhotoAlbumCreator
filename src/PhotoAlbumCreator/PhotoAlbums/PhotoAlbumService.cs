using PhotoAlbumCreator.AlbumLibraries;
using PhotoAlbumCreator.Common;
using PhotoAlbumCreator.Common.Settings;
using PhotoAlbumCreator.PhotoAlbums.Htmls;
using PhotoAlbumCreator.PhotoAlbums.Photos;
using PhotoAlbumCreator.PhotoAlbums.Videos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PhotoAlbumCreator.PhotoAlbums;

public sealed class PhotoAlbumService : AlbumServiceBase
{
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

    public PhotoAlbum Create()
    {
        var albumLibrary = _albumLibraryService.Create(false);
        var albumName = GetAlbumName();
        var photoAlbum = albumLibrary.CreatePhotoAlbum(albumName);

        Directory.CreateDirectory(photoAlbum.FullPath);
        Directory.CreateDirectory(photoAlbum.FilesDirectoryPath);
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
    
    public void Fill()
    {
        var photoAlbum = Create();
        var mediaFiles = LoadMediaFiles(photoAlbum);
        if (mediaFiles.Count == 0)
        {
            Console.WriteLine(
                string.Format(
                    _localization.NoMediaInFileDirectory,
                    photoAlbum.Library.GetRelativePath(photoAlbum.FilesDirectoryPath)));
            return;
        }

        var albumSettings = _appSettingsProvider.LoadAlbumSettings(photoAlbum.Library.SettingsPath);
        var html = File.ReadAllText(photoAlbum.IndexHtmlPath, new UTF8Encoding(false));
        var indexHtmlPage = new IndexHtmlPage(
            html,
            photoAlbum,
            albumSettings.IndexHtml,
            albumSettings.Localization,
            mediaFiles);

        while (indexHtmlPage.HasMediaFiles())
        {
            var actionSymbol = Ask(_localization.AlbumAlreadyContainsItems)
                .ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(actionSymbol))
            {
                Console.WriteLine(_localization.CommandEmptyError);
                continue;
            }
            else if (actionSymbol.StartsWith("c"))
            {
                Console.WriteLine(_localization.OperationCanceled);
                return;
            }
            else if (actionSymbol.StartsWith("y"))
            {
                break;
            }

            Console.WriteLine(_localization.CommandNotRecognized);
        }

        indexHtmlPage.BuildGallery();

        File.Delete(photoAlbum.IndexHtmlPath);
        File.WriteAllText(photoAlbum.IndexHtmlPath, indexHtmlPage.BuildHtml(), new UTF8Encoding(false));

        Console.WriteLine(string.Format(_localization.GalleryUpdated, photoAlbum.Library.GetRelativePath(photoAlbum.IndexHtmlPath)));
    }
    
    private static IReadOnlyList<MediaFile> LoadMediaFiles(PhotoAlbum photoAlbum)
    {
        var mediaFiles = new List<MediaFile>();
        var directoryInfo = new DirectoryInfo(photoAlbum.FilesDirectoryPath);
        foreach (var fileInfo in directoryInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
        {
            var extension = fileInfo.Extension.ToLowerInvariant();
            bool isImage = Photo.IsSupportedExtension(extension);
            bool isVideo = Video.IsSupportedExtension(extension);
            if (!isImage && !isVideo)
                continue;

            mediaFiles.Add(new MediaFile(
                fileInfo.Name,
                fileInfo.FullName,
                isImage,
                fileInfo.LastWriteTime));
        }

        return mediaFiles;
    }

    private string GetAlbumName()
    {
        while (true)
        {
            var name = Ask(_localization.AlbumNameInput);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            Console.WriteLine(_localization.AlbumNameEmptyInputError);
        }
    }
}
