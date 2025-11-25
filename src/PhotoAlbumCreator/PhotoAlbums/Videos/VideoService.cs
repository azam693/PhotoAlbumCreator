using PhotoAlbumCreator.Common;
using PhotoAlbumCreator.Common.Settings;
using PhotoAlbumCreator.PhotoAlbums.Videos.Compressors;
using PhotoAlbumCreator.PhotoAlbums.Videos.Requests;
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

    public void Compress(CompressVideoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (File.Exists(request.Path))
        {
            var fileInfo = new FileInfo(request.Path);
            _compressor.ProcessFile(fileInfo);
        }
        else if (Directory.Exists(request.Path))
        {
            _compressor.ProcessFolder(request.Path);
        }
        else
        {
            Console.WriteLine(_localization.PathNotFound);
        }
    }
}
