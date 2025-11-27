using PhotoAlbumCreator.Common.Settings;
using PhotoAlbumCreator.PhotoAlbums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PhotoAlbumCreator.AlbumLibraries;

public sealed class AlbumLibrary
{
    public const string SystemDirectoryName = "System";
    public const string ReadmeFileName = "README.md";
    public const string ReadmeFileTemplateName = "README_Libraries.md";
    public const string SettingsFileName = "album_settings.json";
    public const string StyleFileName = "styles.css";
    public const string ScriptFileName = "script.js";

    public string RootPath { get; private set; }
    public string SystemPath { get; private set; }
    public string ReadmePath { get; private set; }
    public string SettingsPath { get; private set; }
    public string ScriptPath { get; private set; }
    public string StylePath { get; private set; }

    public AlbumLibrary(string rootPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootPath);

        RootPath = rootPath;
        ReadmePath = Path.Combine(RootPath, ReadmeFileName);
        SystemPath = Path.Combine(RootPath, SystemDirectoryName);
        SettingsPath = Path.Combine(SystemPath, SettingsFileName);
        ScriptPath = Path.Combine(SystemPath, ScriptFileName);
        StylePath = Path.Combine(SystemPath, StyleFileName);
    }

    public PhotoAlbum CreatePhotoAlbum(
        string name,
        IReadOnlyList<MediaFile> mediaFiles,
        IReadOnlyList<PhotoAlbum> albums)
    {
        return new PhotoAlbum(this, name, mediaFiles, albums);
    }

    public string GetRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var currentDirectoryPath = Path.GetFullPath(RootPath) + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(currentDirectoryPath, StringComparison.OrdinalIgnoreCase))
                return fullPath
                    .Substring(currentDirectoryPath.Length);

            return fullPath;
        }
        catch
        {
            return path;
        }
    }
    
    public string CreateSettingsJson(AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(appSettings);

        var albumSettingsJson = new AppSettings(null, null, appSettings.IndexHtml, appSettings.FFmpeg);

        return JsonSerializer.Serialize(albumSettingsJson, AppSettingsJsonContext.CreateJsonContext().AppSettings);
    }
}
