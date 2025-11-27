using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using PhotoAlbumCreator.Common;
using PhotoAlbumCreator.Common.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PhotoAlbumCreator.PhotoAlbums.Htmls;

public sealed class IndexHtmlPage
{
    private record AlbumActionState(
        IReadOnlyList<PhotoAlbum> AlbumsToAdd,
        IReadOnlyList<IElement> ElementsToRemove,
        IReadOnlyList<(IElement Element, PhotoAlbum Album)> ElementsToUpdate);

    private static readonly MediaFile EmptyMediaFile = new MediaFile("Empty", "/", false, DateTime.Now);

    private const string GallerySelector = ".container #gallery";
    private const string AlbumBlockSelector = ".photos.photos--folders";
    private const string AlbumSelector = ".card.folder";
    private const string CardSelector = ".card";
    private const string PhotosSelector = ".photos";
    private const string GroupSelector = ".group";
    
    private readonly PhotoAlbum _album;
    private readonly IndexHtmlSettings _indexHtmlSettings;
    private readonly LocalizationSettings _localizationSettings;    
    private readonly IHtmlDocument _document;
    private readonly IElement _galleryElement;

    private List<IElement> _styleElements;
    private List<IElement> _scriptElements;

    public IndexHtmlPage(
        string html,
        PhotoAlbum album,
        IndexHtmlSettings indexHtmlSettings,
        LocalizationSettings localizationSettings)
    {
        ArgumentNullException.ThrowIfNull(album);
        ArgumentNullException.ThrowIfNull(indexHtmlSettings);
        ArgumentNullException.ThrowIfNull(localizationSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        _album = album;
        _indexHtmlSettings = indexHtmlSettings;
        _localizationSettings = localizationSettings;

        var mediaFileForTemplate = _album.MediaFiles.Count > 0
            ? _album.MediaFiles.OrderBy(m => m.CreatedAt).First()
            : EmptyMediaFile;
        var indexHtmlTemplate = new IndexHtmlTemplate(html, _localizationSettings);
        var processedHtml = indexHtmlTemplate
            .BuildHeader(_album.Name, mediaFileForTemplate)
            .BuildControls()
            .BuildHtml();
        var parser = new HtmlParser();
        _document = parser.ParseDocument(processedHtml);

        _galleryElement = _document.QuerySelector(GallerySelector);
        if (_galleryElement == null)
        {
            throw new AlbumException("Gallery container element not found in HTML template.");
        }

        RefreshStyleList();
        RefreshScriptList();
    }

    private void RefreshStyleList()
    {
        _styleElements = _document
            .QuerySelectorAll("link[rel=stylesheet], link[rel=preload][as=style]")
            .ToList();
    }

    private void RefreshScriptList()
    {
        _scriptElements = _document
            .QuerySelectorAll("script[src]")
            .ToList();
    }

    public IReadOnlyList<string> GetStyles()
    {
        return _styleElements
            .Select(e => e.GetAttribute("href"))
            .ToArray();
    }

    public IReadOnlyList<string> GetScripts()
    {
        return _scriptElements
            .Select(e => e.GetAttribute("src"))
            .ToArray();
    }

    public void AddStyle(string stylePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stylePath);

        var linkElement = _document.CreateElement("link") as IHtmlLinkElement;
        linkElement.Relation = "stylesheet";
        linkElement.Href = stylePath;

        _document.Head.AppendChild(linkElement);
    }

    public void RemoveStyleByPath(string stylePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stylePath);

        var styleElement = _styleElements
            .FirstOrDefault(element => string.Equals(
                element.GetAttribute("href"),
                stylePath,
                StringComparison.OrdinalIgnoreCase));

        if (styleElement is not null)
        {
            styleElement.Remove();
            RefreshStyleList();
        }
    }

    public void AddScript(string scriptPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptPath);

        var scriptElement = _document.CreateElement("script") as IHtmlScriptElement;
        scriptElement.SetAttribute("defer", "");
        scriptElement.Source = scriptPath;

        _document.Head.AppendChild(scriptElement);
    }

    public void RemoveScriptByPath(string scriptPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptPath);

        var scriptElement = _scriptElements
            .FirstOrDefault(element => string.Equals(
                element.GetAttribute("src"),
                scriptPath,
                StringComparison.OrdinalIgnoreCase));

        if (scriptElement is not null)
        {
            scriptElement.Remove();
            RefreshScriptList();
        }
    }

    public string BuildHtml()
    {
        using var stringWriter = new StringWriter();

        _document.ToHtml(stringWriter, new PrettyMarkupFormatter());

        return stringWriter.ToString();
    }

    public bool HasMediaFiles()
    {
        return GetExistingMediaFileElements().Any();
    }

    public void BuildGallery()
    {
        BuildInternalAlbums();
        BuildMediaFiles();
    }

    private void BuildInternalAlbums()
    {
        if (_album.ChildAlbums.Count == 0)
            return;

        var albumsToWrite = _album.ChildAlbums;
        var albumBlock = GetAlbumBlock();
        if (albumBlock is not null)
        {
            var albumElements = albumBlock.QuerySelectorAll(AlbumSelector);
            var albumActionState = BuildAlbumActionState(_album.ChildAlbums, albumElements);
            RemoveAlbumElements(albumActionState.ElementsToRemove);
            UpdateAlbumElements(albumActionState.ElementsToUpdate);

            albumsToWrite = albumActionState.AlbumsToAdd;
        }

        albumBlock = GetAlbumBlock();
        if (albumsToWrite.Any())
        {
            var albumsHtml = CreateAlbumItemsHtml(albumsToWrite);
            if (albumBlock is null)
            {
                var newAlbumBlock = _indexHtmlSettings.AlbumBlock
                    .Replace("{{albumItems}}", albumsHtml);
                var groupAlbumsHtml = _indexHtmlSettings.GroupBlock
                    .Replace("{{groupBlock}}", newAlbumBlock);
                _galleryElement.Insert(AdjacentPosition.AfterBegin, groupAlbumsHtml);
            }
            else
            {
                albumBlock.Insert(AdjacentPosition.BeforeEnd, albumsHtml);
            }
        }
    }

    private AlbumActionState BuildAlbumActionState(
        IReadOnlyList<PhotoAlbum> albums,
        IHtmlCollection<IElement> existingElements)
    {
        var existingElementsPairs = existingElements
            .Select(element =>
            {
                var path = element
                    .QuerySelector("a")
                    .GetAttribute("href");

                var albumName = string.IsNullOrWhiteSpace(path)
                    ? null
                    : Path.GetDirectoryName(path);

                return (AlbumName: albumName, Element: element);
            })
            .Where(obj => obj.AlbumName != null)
            .DistinctBy(obj => obj.AlbumName)
            .ToDictionary(obj => obj.AlbumName, obj => obj.Element);
        var albumPairs = albums
            .ToDictionary(album => album.Name, album => album);

        var albumsToAdd = albumPairs
            .Where(albumPair => !existingElementsPairs.ContainsKey(albumPair.Key))
            .Select(albumPair => albumPair.Value)
            .ToArray();
        var elementsToRemove = existingElementsPairs
            .Where(existingElementsPair => !albumPairs.ContainsKey(existingElementsPair.Key))
            .Select(existingElementsPair => existingElementsPair.Value)
            .ToArray();

        var elementsToUpdate = existingElementsPairs
            .Where(element => !elementsToRemove.Contains(element.Value))
            .Where(element => !albumsToAdd.Any(album => album.Name == element.Key))
            .Where(element => albumPairs.ContainsKey(element.Key))
            .Select(element => (Element: element.Value, Album: albumPairs[element.Key]))
            .ToArray();

        return new(albumsToAdd, elementsToRemove, elementsToUpdate);
    }

    private void RemoveAlbumElements(IReadOnlyList<IElement> albumElements)
    {
        if (albumElements.Count == 0)
            return;

        // Remove albums that are no longer present
        foreach (var element in albumElements)
        {
            element
                .Closest(CardSelector)
                ?.Remove();
        }

        var albumFolder = GetAlbumBlock();
        if (albumFolder is null)
            return;

        if (albumFolder.QuerySelectorAll(AlbumSelector).Any())
            return;

        albumFolder
            .Closest(GroupSelector)
            ?.Remove();
    }

    private void UpdateAlbumElements(IReadOnlyList<(IElement Element, PhotoAlbum Album)> albumElements)
    {
        foreach (var albumElement in albumElements)
        {
            var fileCountElements = albumElement.Element.QuerySelector(".folder-meta");
            if (fileCountElements is not null)
            {
                fileCountElements.InnerHtml = GetMediaFilesCountText(
                    albumElement.Album.MediaFiles.Count);
            }
        }
    }

    private IElement? GetAlbumBlock()
    {
        return _galleryElement.QuerySelector(AlbumBlockSelector);
    }
    
    public string CreateAlbumItemsHtml(IReadOnlyList<PhotoAlbum> photoAlbums)
    {
        if (photoAlbums.Count == 0)
            return string.Empty;

        var itemsBuilder = new StringBuilder();
        foreach (var photoAlbum in photoAlbums)
        {
            itemsBuilder.AppendLine(
                _indexHtmlSettings.AlbumItem
                    .Replace("{{albumPath}}", $"{photoAlbum.Name}/{PhotoAlbum.IndexHtmlFileName}")
                    .Replace("{{albumName}}", photoAlbum.Name))
                    .Replace("{{filesCount}}", GetMediaFilesCountText(photoAlbum.MediaFiles.Count));
        }

        return itemsBuilder.ToString();
    }
    
    private static string GetMediaFilesCountText(int count)
    {
        return $"({count})";
    }

    private void BuildMediaFiles()
    {
        if (_album.MediaFiles.Count == 0)
            return;

        var mediaFilesToWrite = _album.MediaFiles;
        var fileElements = GetExistingMediaFileElements();
        if (fileElements.Any())
        {
            var (filesToAdd, filesToRemove) = FilterExistingMediaFiles(_album.MediaFiles, fileElements);
            RemoveMediaFiles(filesToRemove);

            mediaFilesToWrite = filesToAdd;
        }

        if (mediaFilesToWrite.Any())
        {
            var mediaFileGroups = CreateMediaFileGroups(mediaFilesToWrite);
            var mediaFilesHtml = CreateMediaFilesHtml(mediaFileGroups);

            _galleryElement.Insert(AdjacentPosition.BeforeEnd, mediaFilesHtml);
        }
    }

    private IHtmlCollection<IElement> GetExistingMediaFileElements()
    {
        return _galleryElement.QuerySelectorAll($"{CardSelector} img, {CardSelector} video");
    }

    private (IReadOnlyList<MediaFile> FilesToAdd, IReadOnlyList<IElement> FilesToRemove) FilterExistingMediaFiles(
        IReadOnlyList<MediaFile> mediaFiles,
        IHtmlCollection<IElement> existingElements)
    {
        var existingElementsPairs = existingElements
            .Select(element =>
            {
                var src = element.GetAttribute("src");

                var fileName = string.IsNullOrWhiteSpace(src)
                    ? null
                    : Path.GetFileName(src);

                return (FileName: fileName, Element: element);
            })
            .Where(obj => obj.FileName != null)
            .DistinctBy(obj => obj.FileName)
            .ToDictionary(obj => obj.FileName, obj => obj.Element);
        var mediaFilesPairs = mediaFiles
            .ToDictionary(mediaFile => Path.GetFileName(mediaFile.FullPath), mediaFile => mediaFile);

        var filesToAdd = mediaFilesPairs
            .Where(mediaFilesPair => !existingElementsPairs.ContainsKey(mediaFilesPair.Key))
            .Select(mediaFilesPair => mediaFilesPair.Value)
            .ToArray();
        var filesToRemove = existingElementsPairs
            .Where(existingElementsPair => !mediaFilesPairs.ContainsKey(existingElementsPair.Key))
            .Select(existingElementsPair => existingElementsPair.Value)
            .ToArray();

        return (FilesToAdd: filesToAdd, FilesToRemove: filesToRemove);
    }

    private void RemoveMediaFiles(IReadOnlyList<IElement> mediaFileElements)
    {
        if (mediaFileElements.Count == 0)
            return;

        // Remove cards with media files that are no longer present
        foreach (var element in mediaFileElements)
        {
            element
                .Closest(CardSelector)
                ?.Remove();
        }

        // Remove empty photos containers
        foreach (var photosElement in _galleryElement.QuerySelectorAll(PhotosSelector))
        {
            if (!photosElement.QuerySelectorAll(CardSelector).Any())
            {
                photosElement.Remove();
            }
        }

        // Remove empty groups containers
        foreach (var groupElement in _galleryElement.QuerySelectorAll(GroupSelector))
        {
            if (!groupElement.QuerySelectorAll(PhotosSelector).Any())
            {
                groupElement.Remove();
            }
        }
    }

    public string CreateMediaFilesHtml(IReadOnlyList<IReadOnlyList<MediaFile>> mediaFilesGroups)
    {
        var itemsBuilder = new StringBuilder();
        var groupBuilder = new StringBuilder();
        foreach (var mediaFiles in mediaFilesGroups)
        {
            foreach (var mediaFile in mediaFiles)
            {
                var mediaFilePath = $"{PhotoAlbum.FilesDirectoryName}/{mediaFile.Name}";
                if (mediaFile.IsImage)
                {
                    itemsBuilder.AppendLine(
                        _indexHtmlSettings.ImageItem.Replace(
                            "{{mediaFilePath}}",
                            mediaFilePath));
                }
                else
                {
                    itemsBuilder.AppendLine(
                        _indexHtmlSettings.VideoItem.Replace(
                            "{{mediaFilePath}}",
                            mediaFilePath));
                }
            }

            var mediaBlockBuilder = new StringBuilder();
            mediaBlockBuilder.AppendLine(
                _indexHtmlSettings.MediaBlock.Replace(
                    "{{mediaItems}}",
                    itemsBuilder.ToString()));
            mediaBlockBuilder.AppendLine(_indexHtmlSettings.TextItem);
            groupBuilder.AppendLine(
                _indexHtmlSettings.GroupBlock.Replace(
                    "{{groupBlock}}",
                    mediaBlockBuilder.ToString()));

            itemsBuilder.Clear();
        }

        return groupBuilder.ToString();
    }

    private IReadOnlyList<IReadOnlyList<MediaFile>> CreateMediaFileGroups(IReadOnlyList<MediaFile> mediaFiles)
    {
        return mediaFiles
            .OrderBy(f => f.CreatedAt)
            .ThenBy(f => f.Name)
            .Aggregate(new List<List<MediaFile>>(), (group, file) =>
            {
                if (group.Count == 0)
                {
                    group.Add(new List<MediaFile> { file });
                }
                else
                {
                    var currentGroup = group.Last();
                    var createdAtWindowStart = currentGroup.First().CreatedAt;
                    var timeDifference = file.CreatedAt - createdAtWindowStart;
                    if (timeDifference <= TimeSpan.FromMinutes(_indexHtmlSettings.GroupTimeWindow)
                        && currentGroup.Count < _indexHtmlSettings.CountElementsInGroup)
                    {
                        currentGroup.Add(file);
                    }
                    else
                    {
                        group.Add(new List<MediaFile> { file });
                    }
                }

                return group;
            });
    }
}
