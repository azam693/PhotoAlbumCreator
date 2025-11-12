using PhotoAlbumCreator.Common.Extensions;
using PhotoAlbumCreator.Common.Files;
using PhotoAlbumCreator.Common.Settings;
using System;
using System.IO;
using System.Linq;

namespace PhotoAlbumCreator.PhotoAlbums.Videos.Compressors;

public sealed class VideoCompressor
{
    private readonly ICompressor _compressor;
    private readonly LocalizationSettings _localization;

    public VideoCompressor(
        ICompressor compressor,
        LocalizationSettings localization)
    {
        ArgumentNullException.ThrowIfNull(compressor);
        ArgumentNullException.ThrowIfNull(localization);

        _compressor = compressor;
        _localization = localization;
    }

    public void ProcessFolder(string folderPath)
    {
        var dirInfo = new DirectoryInfo(folderPath);
        var files = dirInfo
            .GetFiles("*", SearchOption.TopDirectoryOnly)
            .Where(file => Video.Extensions.Contains(file.Extension.ToLower()))
            .ToArray();

        if (files.Length == 0)
        {
            Console.WriteLine(_localization.NoVideoInDirectory.Fmt(folderPath));
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(
            _localization.CountOfVideoFilesFound.Fmt(files.Length)
            + " "
            + _localization.StartingCompression);
        Console.ResetColor();

        foreach (var file in files)
        {
            ProcessFile(file);
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(_localization.VideoFilesCompressed);
        Console.ResetColor();
    }

    public void ProcessFile(FileInfo file)
    {
        string originalPath = file.FullName;
        string currentDirectory = file.DirectoryName ?? Directory.GetCurrentDirectory();
        string baseName = Path.GetFileNameWithoutExtension(file.Name);
        string extension = Path.GetExtension(file.Name);

        string temporaryPath = Path.Combine(currentDirectory, baseName + $".__tmp__{extension}");
        string backupPath = Path.Combine(currentDirectory, baseName + $".__old__{extension}");

        var swap = new FileSwapper(file, temporaryPath, backupPath);
        swap.CleanResidual();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($">> {_localization.Processing}: {file.Name}");
        Console.ResetColor();

        _compressor.Compress(originalPath, temporaryPath, VideoQualities.Medium);

        swap.Commit();
    }
}
