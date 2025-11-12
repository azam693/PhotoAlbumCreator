using System;
using System.Diagnostics;

namespace PhotoAlbumCreator.Common;

public sealed class ProcessRunner
{
    public int Run(string fileName, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
            return -1;

        process.WaitForExit();

        return process.ExitCode;
    }
}
