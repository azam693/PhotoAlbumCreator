using PhotoAlbumCreator.AlbumLibraries;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace PhotoAlbumCreator.Common.Settings;

public class AppSettingsProvider
{
    private const string SettingsFileName = "appsettings.json";

    private readonly Resource _resource;

    private AppSettings? _cache;

    public AppSettingsProvider(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        _resource = resource;
    }

    public AppSettings LoadMainSettings()
    {
        if (_cache is not null)
            return _cache;

        var appSettingsJson = _resource.ReadText(SettingsFileName);
        if (string.IsNullOrWhiteSpace(appSettingsJson))
        {
            throw new AlbumException($"{AlbumLibrary.SettingsFileName} can't be empty.");
        }

        _cache = JsonSerializer.Deserialize(appSettingsJson, AppSettingsJsonContext.Default.AppSettings)
            ?? throw new AlbumException($"Can't deserialize {AlbumLibrary.SettingsFileName}");
        
        return _cache;
    }

    public AppSettings LoadAlbumSettings(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var mainSettings = LoadMainSettings();
        var appSettingsJson = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(appSettingsJson))
        {
            return mainSettings;
        }

        var albumSettings = JsonSerializer.Deserialize(appSettingsJson, AppSettingsJsonContext.CreateJsonContext().AppSettings);
        if (albumSettings is null)
        {
            return mainSettings;
        }

        return mainSettings with
        {
            IndexHtml = albumSettings.IndexHtml
        };
    }
}
