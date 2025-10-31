using PhotoAlbumCreator.AlbumLibraries;
using PhotoAlbumCreator.Common;
using PhotoAlbumCreator.Common.Settings;
using PhotoAlbumCreator.PhotoAlbums;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

internal static class Program
{
    public const string CreateAlbumLibrary = "init";
    public const string CreatePhotoAlbum = "new";
    public const string FillPhotoAlbum = "fill";
    public const string Help = "help";

    public const string ForceParameter = "--force";

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var commands = args;
        var resource = new Resource();
        var appSettingsProvider = new AppSettingsProvider(resource);
        var appSettings = appSettingsProvider.LoadMainSettings();
        var localization = appSettings.Localization;

        var culture = new CultureInfo(appSettings.CultureInfo);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
#if DEBUG

        Console.Write(localization.CommandInput);

        commands = Console.ReadLine()
            .Trim()
            .Split(' ')
            .Select(text => text.ToLowerInvariant())
            .ToArray();

#endif

        if (commands.Length == 0)
        {
            Console.WriteLine(localization.CommandNotSpecified);
            Console.WriteLine(localization.Help);
            return 1;
        }

        try
        {
            var albumLibraryService = new AlbumLibraryService(resource, appSettingsProvider);
            var photoAlbumService = new PhotoAlbumService(resource, appSettingsProvider, albumLibraryService);
            switch (commands[0])
            {
                case CreateAlbumLibrary:
                    bool isForce = commands.Any(a => string.Equals(a, ForceParameter, StringComparison.OrdinalIgnoreCase));
                    albumLibraryService.Create(isForce: isForce);
                    break;
                case CreatePhotoAlbum:
                    photoAlbumService.Create();
                    break;
                case FillPhotoAlbum:
                    photoAlbumService.Fill();
                    break;
                case Help:
                case $"--{Help}":
                case "-h":
                    Console.WriteLine(localization.Help);
                    break;
                default:
                    Console.WriteLine(localization.CommandNotRecognized);
                    Console.WriteLine(localization.Help);
                    return 2;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(localization.Error + ex.Message);
            return 3;
        }
    }
}
