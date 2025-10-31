using System;
using System.IO;
using System.Text;

namespace PhotoAlbumCreator.Common;

public sealed class Resource
{
    private const string ResourcesFolderName = "Resources";

    public string ReadText(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        var relativePath = resourceName
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        string fullPath = Path.Combine(
            AppContext.BaseDirectory,
            ResourcesFolderName,
            relativePath);

        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                $"Resource with a name \"{resourceName}\" doesn't exist at the path: {fullPath}");
        }

        return File.ReadAllText(fullPath, Encoding.UTF8);
    }
}
