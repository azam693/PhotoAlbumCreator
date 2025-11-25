using PhotoAlbumCreator.AlbumLibraries.Requests;
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

    public AlbumLibrary Create(CreateAlbumLibraryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var albumLibrary = new AlbumLibrary(request.RootPath);
        Directory.CreateDirectory(albumLibrary.SystemPath);
        // Create styles file
        CreateFile(
            albumLibrary.StylePath,
            albumLibrary.GetRelativePath(albumLibrary.StylePath),
            _resource.ReadText(AlbumLibrary.StyleFileName),
            request.IsForce);
        // Create script file
        CreateFile(
            albumLibrary.ScriptPath,
            albumLibrary.GetRelativePath(albumLibrary.ScriptPath),
            _resource.ReadText(AlbumLibrary.ScriptFileName),
            request.IsForce);
        // Create README file
        CreateFile(
            albumLibrary.ReadmePath,
            albumLibrary.GetRelativePath(albumLibrary.ReadmePath),
            _resource.ReadText(AlbumLibrary.ReadmeFileTemplateName),
            request.IsForce);
        // Create settings file
        CreateFile(
            albumLibrary.SettingsPath,
            albumLibrary.GetRelativePath(albumLibrary.SettingsPath),
            albumLibrary.CreateSettingsJson(_appSettings),
            request.IsForce);

        return albumLibrary;
    }
}
