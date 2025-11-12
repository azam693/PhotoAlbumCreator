using PhotoAlbumCreator.Common;
using PhotoAlbumCreator.Common.Settings;
using PhotoAlbumCreator.PhotoAlbums.Videos.Compressors;
using System;
using System.IO;

namespace PhotoAlbumCreator.PhotoAlbums.Videos;

public sealed class VideoService : AlbumServiceBase
{
    private readonly VideoCompressor _compressor;

    public VideoService(VideoCompressor compressor, AppSettingsProvider appSettingsProvider)
        : base(appSettingsProvider)
    {
        ArgumentNullException.ThrowIfNull(compressor);

        _compressor = compressor;
    }

    public void Compress()
    {
        Console.WriteLine(_localization.OnlyVideoFormatsSupports);

        var path = Ask(_localization.PathInput);
        
        if (File.Exists(path))
        {
            var fileInfo = new FileInfo(path);
            _compressor.ProcessFile(fileInfo);
        }
        else if (Directory.Exists(path))
        {
            _compressor.ProcessFolder(path);
        }
        else
        {
            Console.WriteLine(_localization.PathNotFound);
        }
    }
}
