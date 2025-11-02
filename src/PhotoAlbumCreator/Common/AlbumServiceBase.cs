using PhotoAlbumCreator.Common.Settings;
using System;
using System.IO;
using System.Text;

namespace PhotoAlbumCreator.Common;

public abstract class AlbumServiceBase
{
    protected readonly AppSettingsProvider _appSettingsProvider;
    protected readonly AppSettings _appSettings;
    protected readonly LocalizationSettings _localization;

    protected AlbumServiceBase(AppSettingsProvider appSettingsProvider)
    {
        ArgumentNullException.ThrowIfNull(appSettingsProvider);

        _appSettingsProvider = appSettingsProvider;
        _appSettings = appSettingsProvider.LoadMainSettings();
        _localization = _appSettings.Localization;
    }

    protected void CreateFile(
        string fullpath,
        string relativePath,
        string text,
        bool isForce = false)
    {
        if (!File.Exists(fullpath) || isForce)
        {
            File.WriteAllText(fullpath, text, new UTF8Encoding(false));

            Console.WriteLine(string.Format(_localization.FileAdded, relativePath));
        }
        else
        {
            Console.WriteLine(string.Format(_localization.FileFound, relativePath));
        }
    }

    protected string Ask(string prompt)
    {
        Console.Write(prompt + ' ');

        return Console.ReadLine().Trim() ?? string.Empty;
    }
}
