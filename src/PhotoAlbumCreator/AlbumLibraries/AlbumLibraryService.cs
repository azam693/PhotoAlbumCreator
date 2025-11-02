using PhotoAlbumCreator.Common;
using PhotoAlbumCreator.Common.Settings;
using System;
using System.IO;

namespace PhotoAlbumCreator.AlbumLibraries;

public sealed class AlbumLibraryService : AlbumServiceBase
{
    private readonly Resource _resource;

    public AlbumLibraryService(
        Resource resource,
        AppSettingsProvider appSettingsProvider)
        : base(appSettingsProvider)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(appSettingsProvider);

        _resource = resource;
    }

    public AlbumLibrary Create(bool isForce)
    {
        var rootPath = GetRootPath();
        var albumLibrary = new AlbumLibrary(rootPath);
        Directory.CreateDirectory(albumLibrary.SystemPath);
        // Create styles file
        CreateFile(
            albumLibrary.StylePath,
            albumLibrary.GetRelativePath(albumLibrary.StylePath),
            _resource.ReadText(AlbumLibrary.StyleFileName),
            isForce);
        // Create script file
        CreateFile(
            albumLibrary.ScriptPath,
            albumLibrary.GetRelativePath(albumLibrary.ScriptPath),
            _resource.ReadText(AlbumLibrary.ScriptFileName),
            isForce);
        // Create README file
        CreateFile(
            albumLibrary.ReadmePath,
            albumLibrary.GetRelativePath(albumLibrary.ReadmePath),
            _resource.ReadText(AlbumLibrary.ReadmeFileTemplateName),
            isForce);
        // Create settings file
        CreateFile(
            albumLibrary.SettingsPath,
            albumLibrary.GetRelativePath(albumLibrary.SettingsPath),
            albumLibrary.CreateSettingsJson(_appSettings),
            isForce);

        return albumLibrary;
    }

    private string GetRootPath()
    {
        while (true)
        {
            var input = Ask(_localization.RootPathInput);
            if (string.IsNullOrWhiteSpace(input))
            {
                return Directory.GetCurrentDirectory();
            }

            if (Directory.Exists(input))
            {
                return Path.GetFullPath(input);
            }

            Console.WriteLine(_localization.DirectoryNotFound);
        }
    }
}
