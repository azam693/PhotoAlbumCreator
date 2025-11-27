using System;
using System.IO;
using System.Text;
using Xunit;
using PhotoAlbumCreator.AlbumLibraries;
using PhotoAlbumCreator.Common;
using PhotoAlbumCreator.Common.Settings;
using PhotoAlbumCreator.PhotoAlbums;
using PhotoAlbumCreator.AlbumLibraries.Requests;
using PhotoAlbumCreator.PhotoAlbums.Requests;

namespace PhotoAlbumCreator.Tests;

public class AlbumCommandsIntegrationTests
{
    private const string TestProjectName = "PhotoAlbumCreator.Tests";
    private const string ResourceFolder = "Resources";

    [Fact]
    public void InitCommand_CreatesSystemAndFiles()
    {
        var resources = CopyResourcesToAppBase();
        var tempRoot = Path.Combine(Path.GetTempPath(), TestProjectName, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        try
        {
            var resource = new Resource();
            var appSettingsProvider = new AppSettingsProvider(resource);
            var albumLibraryService = new AlbumLibraryService(resource, appSettingsProvider);
            var request = new CreateAlbumLibraryRequest(tempRoot, isForce: false);
            var albumLibrary = albumLibraryService.Create(request);

            // Assert System folder exists
            Assert.True(Directory.Exists(albumLibrary.SystemPath));

            // Expected files
            var stylePath = albumLibrary.StylePath;
            var scriptPath = albumLibrary.ScriptPath;
            var settingsPath = albumLibrary.SettingsPath;

            Assert.True(File.Exists(stylePath));
            Assert.True(File.Exists(scriptPath));
            Assert.True(File.Exists(settingsPath));

            // Compare contents with resource files created earlier
            var expectedStyle = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, ResourceFolder, AlbumLibrary.StyleFileName));
            var expectedScript = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, ResourceFolder, AlbumLibrary.ScriptFileName));
            var actualStyle = File.ReadAllText(stylePath);
            var actualScript = File.ReadAllText(scriptPath);

            Assert.Equal(expectedStyle, actualStyle);
            Assert.Equal(expectedScript, actualScript);
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }

    [Fact]
    public void NewCommand_CreatesAlbumAndIndexHtml()
    {
        var resources = CopyResourcesToAppBase();
        var tempRoot = Path.Combine(Path.GetTempPath(), TestProjectName, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        try
        {
            var resource = new Resource();
            var appSettingsProvider = new AppSettingsProvider(resource);
            var albumLibraryService = new AlbumLibraryService(resource, appSettingsProvider);
            var photoAlbumService = new PhotoAlbumService(resource, appSettingsProvider, albumLibraryService);

            var createLibraryRequest = new CreateAlbumLibraryRequest(tempRoot, isForce: false);
            var createAlbumRequest = new CreatePhotoAlbumRequest(tempRoot, "MyAlbum");

            // Ensure library exists
            albumLibraryService.Create(createLibraryRequest);

            var album = photoAlbumService.Create(createAlbumRequest);

            // Check system files
            Assert.True(Directory.Exists(album.Library.SystemPath));
            Assert.True(File.Exists(album.Library.StylePath));
            Assert.True(File.Exists(album.Library.ScriptPath));
            Assert.True(File.Exists(album.Library.SettingsPath));

            // Check album folder and Files and index.html
            Assert.True(Directory.Exists(album.FullPath));
            Assert.True(Directory.Exists(album.FilesDirectoryPath));
            Assert.True(File.Exists(album.IndexHtmlPath));

            // Compare index.html to resource template
            var expectedIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, ResourceFolder, PhotoAlbum.IndexHtmlTemplateName));
            var actualIndex = File.ReadAllText(album.IndexHtmlPath);
            Assert.Equal(expectedIndex, actualIndex);
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }

    [Fact]
    public void FillCommand_PopulatesIndexWithMediaFiles()
    {
        const string albumName = "FillAlbum";

        var resources = CopyResourcesToAppBase();
        var tempRoot = Path.Combine(Path.GetTempPath(), TestProjectName, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        var albumDirectory = Path.Combine(tempRoot, albumName);
        var filesDirectory = Path.Combine(albumDirectory, PhotoAlbum.FilesDirectoryName);
        Directory.CreateDirectory(filesDirectory);

        // Create fake image files
        var file1 = Path.Combine(filesDirectory, "IMG_001.jpg");
        var file2 = Path.Combine(filesDirectory, "IMG_002.jpg");
        File.WriteAllText(file1, "fakejpgcontent");
        File.WriteAllText(file2, "fakejpgcontent");

        try
        {
            var resource = new Resource();
            var appSettingsProvider = new AppSettingsProvider(resource);
            var albumLibraryService = new AlbumLibraryService(resource, appSettingsProvider);
            var photoAlbumService = new PhotoAlbumService(resource, appSettingsProvider, albumLibraryService);

            // Call Fill - it will create System and index.html then scan Files and build gallery
            var fillRequest = new FillPhotoAlbumRequest(tempRoot, albumName);
            photoAlbumService.Fill(fillRequest);

            var indexPath = Path.Combine(albumDirectory, PhotoAlbum.IndexHtmlFileName);
            Assert.True(File.Exists(indexPath));

            var indexContent = File.ReadAllText(indexPath);
            // It should contain references to the files inside Files/ directory
            Assert.Contains("Files/IMG_001.jpg", indexContent);
            Assert.Contains("Files/IMG_002.jpg", indexContent);
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }

    private static string CopyResourcesToAppBase()
    {
        var target = Path.Combine(AppContext.BaseDirectory, ResourceFolder);
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);

        // find repository root by searching for PhotoAlbumCreator.csproj in ancestors (handles layout with src/)
        string? repoRoot = null;
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PhotoAlbumCreator.csproj")))
            {
                repoRoot = dir.FullName;
                break;
            }

            // also handle repo layout where projects are under 'src' folder
            if (File.Exists(Path.Combine(dir.FullName, "src", "PhotoAlbumCreator", "PhotoAlbumCreator.csproj")))
            {
                repoRoot = dir.FullName;
                break;
            }

            dir = dir.Parent;
        }

        if (repoRoot is null)
            throw new InvalidOperationException("Can't locate repository root. Run tests from repository checkout.");

        // possible locations for Resources
        var candidates = new[]
        {
            Path.Combine(repoRoot, "src", "PhotoAlbumCreator", ResourceFolder),
            Path.Combine(repoRoot, "PhotoAlbumCreator", ResourceFolder),
            Path.Combine(repoRoot, "src", "PhotoAlbumCreator", "Resources"), // alternative spelling
            Path.Combine(repoRoot, "PhotoAlbumCreator", "Resources")
        };

        string? srcResources = null;
        foreach (var c in candidates)
        {
            if (Directory.Exists(c))
            {
                srcResources = c;
                break;
            }
        }

        if (srcResources is null)
            throw new InvalidOperationException("Can't locate source Resources folder. Ensure tests run from repository checkout.");

        foreach (var file in Directory.EnumerateFiles(srcResources))
        {
            var fileName = Path.GetFileName(file);
            var fileDestination = Path.Combine(target, fileName);
            File.Copy(file, fileDestination, overwrite: true);
        }

        return target;
    }
}
