using PhotoAlbumCreator.Common;
using System;
using System.IO;

namespace PhotoAlbumCreator.PhotoAlbums.Videos.Compressors.FFmpeg;

public sealed class FFmpegCompressor : ICompressor
{
    private readonly ProcessRunner _processRunner;
    private readonly FFmpegSettings _settings;

    public FFmpegCompressor(
        ProcessRunner processRunner,
        FFmpegSettings settings)
    {
        ArgumentNullException.ThrowIfNull(processRunner);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Path);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Arguments);

        _processRunner = processRunner;
        _settings = settings;

        if (_settings.Audio is null)
        {
            _settings = _settings with
            {
                Audio = FFmpegAudioSettings.CreateDefault()
            };
        }
    }
    
    public bool Compress(string inputPath, string outputPath, VideoQualities quality)
    {
        try
        {
            return CompressImpl(inputPath, outputPath, quality, copyAudio: true)
                || CompressImpl(inputPath, outputPath, quality, copyAudio: false);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Can't run FFmpeg: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    private bool CompressImpl(
        string inputPath,
        string outputPath,
        VideoQualities quality,
        bool copyAudio)
    {
        string audioPart = copyAudio
            ? "-c:a copy"
            : $"-c:a {_settings.Audio.Codec} -b:a {_settings.Audio.BitrateKbps}k";

        string arguments = _settings.Arguments
            .Replace("{{inputPath}}", Quote(inputPath))
            .Replace("{{crf}}", GetConstantRateFactor(quality))
            .Replace("{{audio}}", audioPart)
            .Replace("{{outputPath}}", Quote(outputPath));

        return _processRunner.Run(_settings.Path, arguments) == 0
            && File.Exists(outputPath);
    }

    private static string GetConstantRateFactor(VideoQualities quality)
    {
        return quality switch
        {
            VideoQualities.Low => "28",
            VideoQualities.Medium => "20",
            VideoQualities.High => "17",
            _ => throw new AlbumException("Unrecognized video quality.")
        };
    }

    private static string Quote(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "\"\"";

        if (path.Contains(' ') || path.Contains('\t') || path.Contains('"'))
        {
            path = path.Replace("\"", "\\\"");
            return $"\"{path}\"";
        }

        return path;
    }
}
