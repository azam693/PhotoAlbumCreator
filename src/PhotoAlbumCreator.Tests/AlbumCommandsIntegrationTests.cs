using PhotoAlbumCreator.AlbumLibraries;
using PhotoAlbumCreator.Common;
using PhotoAlbumCreator.Common.Settings;
using PhotoAlbumCreator.PhotoAlbums;

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

        var input = new StringReader(tempRoot + Environment.NewLine);
        var originalIn = Console.In;
        try
        {
            Console.SetIn(input);

            var resource = new Resource();
            var appSettingsProvider = new AppSettingsProvider(resource);
            var albumLibraryService = new AlbumLibraryService(resource, appSettingsProvider);
            var albumLibrary = albumLibraryService.Create(isForce: false);

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
            Console.SetIn(originalIn);
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void NewCommand_CreatesAlbumAndIndexHtml()
    {
        var resources = CopyResourcesToAppBase();
        var tempRoot = Path.Combine(Path.GetTempPath(), TestProjectName, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        var input = new StringReader(tempRoot + Environment.NewLine + "MyAlbum" + Environment.NewLine);
        var originalIn = Console.In;
        try
        {
            Console.SetIn(input);

            var resource = new Resource();
            var appSettingsProvider = new AppSettingsProvider(resource);
            var albumLibraryService = new AlbumLibraryService(resource, appSettingsProvider);
            var photoAlbumService = new PhotoAlbumService(resource, appSettingsProvider, albumLibraryService);

            var album = photoAlbumService.Create();

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
            Console.SetIn(originalIn);
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch { }
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

        var input = new StringReader(tempRoot + Environment.NewLine + albumName + Environment.NewLine);
        var originalIn = Console.In;
        try
        {
            Console.SetIn(input);

            var resource = new Resource();
            var appSettingsProvider = new AppSettingsProvider(resource);
            var albumLibraryService = new AlbumLibraryService(resource, appSettingsProvider);
            var photoAlbumService = new PhotoAlbumService(resource, appSettingsProvider, albumLibraryService);

            // Call Fill - it will create System and index.html then scan Files and build gallery
            photoAlbumService.Fill();

            var indexPath = Path.Combine(albumDirectory, PhotoAlbum.IndexHtmlFileName);
            Assert.True(File.Exists(indexPath));

            var indexContent = File.ReadAllText(indexPath);
            // It should contain references to the files inside Files/ directory
            Assert.Contains("Files/IMG_001.jpg", indexContent);
            Assert.Contains("Files/IMG_002.jpg", indexContent);
        }
        finally
        {
            Console.SetIn(originalIn);
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch { }
        }
    }

    private static string CopyResourcesToAppBase()
    {
        var target = Path.Combine(AppContext.BaseDirectory, ResourceFolder);
        if (!Directory.Exists(target))
        {
            Directory.CreateDirectory(target);
        }

        // Try to locate source resources folder in the repo: ../src/PhotoAlbumCreator/Resources
        string? srcResources = null;
        var currentDirectory = Directory.GetCurrentDirectory();
        var directoryInfo = new DirectoryInfo(currentDirectory);
        while (directoryInfo != null)
        {
            var candidate = Path.Combine(directoryInfo.FullName, "src", "PhotoAlbumCreator", ResourceFolder);
            if (Directory.Exists(candidate))
            {
                srcResources = candidate;
                break;
            }
            directoryInfo = directoryInfo.Parent;
        }

        if (srcResources is null)
        {
            throw new InvalidOperationException("Can't locate source Resources folder. Ensure tests run from repository checkout.");
        }

        foreach (var file in Directory.EnumerateFiles(srcResources))
        {
            var fileName = Path.GetFileName(file);
            var fileDestination = Path.Combine(target, fileName);
            File.Copy(file, fileDestination, overwrite: true);
        }

        return target;
    }
}
