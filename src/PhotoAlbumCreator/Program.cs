using PhotoAlbumCreator.AlbumLibraries;
using PhotoAlbumCreator.AlbumLibraries.Requests;
using PhotoAlbumCreator.Common;
using PhotoAlbumCreator.Common.Extensions;
using PhotoAlbumCreator.Common.Settings;
using PhotoAlbumCreator.PhotoAlbums;
using PhotoAlbumCreator.PhotoAlbums.Requests;
using PhotoAlbumCreator.PhotoAlbums.Videos;
using PhotoAlbumCreator.PhotoAlbums.Videos.Compressors;
using PhotoAlbumCreator.PhotoAlbums.Videos.Compressors.FFmpeg;
using PhotoAlbumCreator.PhotoAlbums.Videos.Requests;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

internal static class Program
{
    public const string CreateAlbumLibrary = "init";
    public const string CreatePhotoAlbum = "new";
    public const string FillPhotoAlbum = "fill";
    public const string Compress = "compress";
    public const string Help = "help";

    public const string ForceParameter = "--force";
    public const string ForceParameterShort = "-f";
    public const string GlobalParameter = "--global";
    public const string GlobalParameterShort = "-g";
    public const string OrderAlbumParameter = "--order-album";
    public const string OrderAlbumParameterShort = "-oa";

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
            var processRunner = new ProcessRunner();
            var albumLibraryService = new AlbumLibraryService(resource, appSettingsProvider);
            var photoAlbumService = new PhotoAlbumService(resource, appSettingsProvider, albumLibraryService);
            switch (commands[0])
            {
                case CreateAlbumLibrary:
                    albumLibraryService.Create(CreateAlbumLibraryRequest());
                    break;
                case CreatePhotoAlbum:
                    photoAlbumService.Create(CreatePhotoAlbumRequest());
                    break;
                case FillPhotoAlbum:
                    var isGlobal = HasParameter(GlobalParameter)
                        || HasParameter(GlobalParameterShort);
                    var orderAlbumValue = GetParamterValue(OrderAlbumParameter)
                        ?? GetParamterValue(OrderAlbumParameterShort);
                    var orderAlbumField = OrderAlbumFields.Date;
                    if (!string.IsNullOrWhiteSpace(orderAlbumValue)
                        && !Enum.TryParse(orderAlbumValue, true, out orderAlbumField))
                    {
                        Console.WriteLine(
                            localization.UnrecognizedOrderAlbumField.Fmt(orderAlbumValue));
                    }

                    if (isGlobal)
                    {
                        photoAlbumService.FillGlobal(FillGlobalPhotoAlbumRequest(orderAlbumField));
                    }
                    else
                    {
                        photoAlbumService.Fill(FillPhotoAlbumRequest(orderAlbumField));
                    }
                    break;
                case Compress:
                    var compressor = new FFmpegCompressor(processRunner, appSettings.FFmpeg);
                    var videoCompressor = new VideoCompressor(compressor, appSettings.Localization);
                    var videoService = new VideoService(videoCompressor, appSettingsProvider);
                    videoService.Compress(CompressVideoRequest());
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
            Console.WriteLine(string.Format(localization.Error, ex.Message));
            return 3;
        }

        CreateAlbumLibraryRequest CreateAlbumLibraryRequest()
        {
            var rootPath = GetRootPath();
            var isForce = HasParameter(ForceParameter)
                || HasParameter(ForceParameterShort);

            return new CreateAlbumLibraryRequest(rootPath, isForce);
        }

        CreatePhotoAlbumRequest CreatePhotoAlbumRequest()
        {
            var rootPath = GetRootPath();
            var albumName = GetAlbumName();
            
            return new CreatePhotoAlbumRequest(rootPath, albumName);
        }

        FillPhotoAlbumRequest FillPhotoAlbumRequest(OrderAlbumFields orderAlbumField)
        {
            var rootPath = GetRootPath();
            var albumName = GetAlbumName();

            return new FillPhotoAlbumRequest(rootPath, albumName, orderAlbumField);
        }

        FillGlobalPhotoAlbumRequest FillGlobalPhotoAlbumRequest(OrderAlbumFields orderAlbumField)
        {
            var rootPath = GetRootPath();

            return new FillGlobalPhotoAlbumRequest(rootPath, orderAlbumField);
        }

        CompressVideoRequest CompressVideoRequest()
        {
            Console.WriteLine(localization.OnlyVideoFormatsSupports);

            var path = Ask(localization.PathInput);

            return new CompressVideoRequest(path);
        }

        string GetAlbumName()
        {
            while (true)
            {
                var name = Ask(localization.AlbumNameInput);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }

                Console.WriteLine(localization.AlbumNameEmptyInputError);
            }
        }

        string GetRootPath()
        {
            while (true)
            {
                var input = Ask(localization.RootPathInput);
                if (string.IsNullOrWhiteSpace(input))
                {
                    return Directory.GetCurrentDirectory();
                }

                if (Directory.Exists(input))
                {
                    return Path.GetFullPath(input);
                }

                Console.WriteLine(localization.DirectoryNotFound);
            }
        }

        string Ask(string prompt)
        {
            Console.Write(prompt + ' ');

            return Console.ReadLine().Trim() ?? string.Empty;
        }

        bool HasParameter(string parameter)
        {
            return commands.Any(
                command => IsEquivalentParameters(command, parameter));
        }

        string? GetParamterValue(string parameter)
        {
            for (var i = 0; i < commands.Length; i++)
            {
                if (!IsEquivalentParameters(commands[i], parameter))
                    continue;

                var nextIndex = i + 1;
                if (nextIndex >= commands.Length)
                    continue;

                return commands[nextIndex];
            }

            return null;
        }

        bool IsEquivalentParameters(string parameter1, string parameter2)
        {
            return string.Equals(parameter1, parameter2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
