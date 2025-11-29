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

    public void BuildGallery(OrderAlbumFields orderAlbumField)
    {
        BuildInternalAlbums(orderAlbumField);
        BuildMediaFiles();
    }

    private void BuildInternalAlbums(OrderAlbumFields orderAlbumField)
    {
        if (_album.ChildAlbums.Count == 0)
            return;

        var albumBlock = _galleryElement.QuerySelector(AlbumBlockSelector);
        if (albumBlock is not null)
        {
            albumBlock.InnerHtml = string.Empty;
        }

        var orderedAlbums = SortPhotoAlbum(orderAlbumField);
        var albumItemsHtml = new StringBuilder();
        foreach (var album in orderedAlbums)
        {
            var albumItemHtml = _indexHtmlSettings.AlbumItem
                .Replace("{{albumPath}}", $"{album.Name}/{PhotoAlbum.IndexHtmlFileName}")
                .Replace("{{albumName}}", album.Name)
                .Replace("{{filesCount}}", $"({album.MediaFiles.Count})");

            albumItemsHtml.Append(albumItemHtml);
        }

        var newAlbumBlockHtml = _indexHtmlSettings.AlbumBlock
            .Replace("{{albumItems}}", albumItemsHtml.ToString());
        var groupAlbumsHtml = _indexHtmlSettings.GroupBlock
            .Replace("{{groupBlock}}", newAlbumBlockHtml);

        _galleryElement.Insert(AdjacentPosition.AfterBegin, groupAlbumsHtml);
    }

    private IReadOnlyList<PhotoAlbum> SortPhotoAlbum(OrderAlbumFields orderAlbumField)
    {
        var dateAlbumPairs = _album.ChildAlbums
            .Select(album => (
                Date: album.MediaFiles
                    .OrderBy(mediaFile => mediaFile.CreatedAt)
                    .FirstOrDefault()
                    ?.CreatedAt,
                Album: album));
        var orderedDateAlbumPairs = orderAlbumField == OrderAlbumFields.Date
            ? dateAlbumPairs
                .OrderBy(obj => obj.Date)
                .ThenBy(obj => obj.Album.Name)
            : dateAlbumPairs
                .OrderBy(obj => obj.Album.Name);

        return orderedDateAlbumPairs
            .Select(obj => obj.Album)
            .ToArray();
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
