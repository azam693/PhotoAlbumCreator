using AngleSharp.Dom;
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
    private const string GallerySelector = ".container #gallery";
    private const string CardSelector = ".card";
    private const string PhotosSelector = ".photos";
    private const string GroupSelector = ".group";
    
    private readonly PhotoAlbum _photoAlbum;
    private readonly IndexHtmlSettings _indexHtmlSettings;
    private readonly LocalizationSettings _localizationSettings;
    private readonly IReadOnlyList<MediaFile> _mediaFiles;
    private readonly HtmlPage _htmlPage;
    private readonly IElement _galleryElement;

    public IndexHtmlPage(
        string html,
        PhotoAlbum photoAlbum,
        IndexHtmlSettings indexHtmlSettings,
        LocalizationSettings localizationSettings,
        IReadOnlyList<MediaFile> mediaFiles)
    {
        ArgumentNullException.ThrowIfNull(photoAlbum);
        ArgumentNullException.ThrowIfNull(indexHtmlSettings);
        ArgumentNullException.ThrowIfNull(localizationSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        _photoAlbum = photoAlbum;
        _indexHtmlSettings = indexHtmlSettings;
        _localizationSettings = localizationSettings;
        _mediaFiles = mediaFiles ?? Array.Empty<MediaFile>();

        var firstMediaFile = _mediaFiles.OrderBy(m => m.CreatedAt).First();
        var indexHtmlTemplate = new IndexHtmlTemplate(html, _localizationSettings);
        var processedHtml = indexHtmlTemplate
            .BuildHeader(_photoAlbum.Name, firstMediaFile)
            .BuildControls()
            .BuildHtml();
        _htmlPage = new HtmlPage(processedHtml);

        _galleryElement = _htmlPage.GetElement(GallerySelector);
        if (_galleryElement == null)
        {
            throw new AlbumException("Gallery container element not found in HTML template.");
        }
    }

    public string BuildHtml()
    {
        return _htmlPage.BuildHtml();
    }

    public bool HasMediaFiles()
    {
        return GetExistingMediaFileElements().Any();
    }

    public void BuildGallery()
    {
        if (_mediaFiles.Count == 0)
            return;

        var mediaFilesToWrite = _mediaFiles;
        var fileElements = GetExistingMediaFileElements();
        if (fileElements.Any())
        {
            var (filesToAdd, filesToDelete) = FilterExistingMediaFiles(_mediaFiles, fileElements);
            RemoveMediaFiles(filesToDelete);

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

    private (IReadOnlyList<MediaFile> FilesToAdd, IReadOnlyList<IElement> FilesToDelete) FilterExistingMediaFiles(
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
        var filesToDelete = existingElementsPairs
            .Where(existingElementsPair => !mediaFilesPairs.ContainsKey(existingElementsPair.Key))
            .Select(existingElementsPair => existingElementsPair.Value)
            .ToArray();

        return (FilesToAdd: filesToAdd, FilesToDelete: filesToDelete);
    }

    private void RemoveMediaFiles(IReadOnlyList<IElement> mediaFileElements)
    {
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
