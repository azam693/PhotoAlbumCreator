using PhotoAlbumCreator.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PhotoAlbumCreator.PhotoAlbums.Htmls;

public sealed class IndexHtmlPage
{
    private readonly PhotoAlbum _photoAlbum;
    private readonly IndexHtmlSettings _indexHtmlSettings;
    private readonly LocalizationSettings _localizationSettings;
    private readonly IReadOnlyList<MediaFile> _mediaFiles;

    private StringBuilder _htmlBuilder;
    
    [JsonIgnore]
    public string Html => _htmlBuilder.ToString();

    public IndexHtmlPage(
        PhotoAlbum photoAlbum,
        IndexHtmlSettings indexHtmlSettings,
        LocalizationSettings localizationSettings,
        string html,
        IReadOnlyList<MediaFile> mediaFiles)
    {
        ArgumentNullException.ThrowIfNull(photoAlbum);
        ArgumentNullException.ThrowIfNull(indexHtmlSettings);
        ArgumentNullException.ThrowIfNull(localizationSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        _photoAlbum = photoAlbum;
        _indexHtmlSettings = indexHtmlSettings;
        _localizationSettings = localizationSettings;
        _htmlBuilder = new StringBuilder(html);
        _mediaFiles = mediaFiles ?? Array.Empty<MediaFile>();
    }

    public bool HasMediaFiles()
    {
        return Regex.IsMatch(
            Html,
            _indexHtmlSettings.MediaFilesExistPattern,
            RegexOptions.IgnoreCase);
    }

    public void BuildGallery()
    {
        if (_mediaFiles.Count == 0)
            return;

        BuildHeader();
        BuildControls();
        BuildItems();

        var htmlPrettifier = new HtmlPrettifier();
        _htmlBuilder = new StringBuilder(htmlPrettifier.Format(Html));
    }

    private void BuildHeader()
    {
        var firstMediaFile = _mediaFiles.OrderBy(m => m.CreatedAt).First();

        _htmlBuilder
            .Replace("{{title}}", _photoAlbum.Name)
            .Replace("{{createdAtIso}}", firstMediaFile.GetCreatedAtISO())
            .Replace("{{createdAtHuman}}", firstMediaFile.GetCreatedAtHuman())
            .Replace("{{published}}", _localizationSettings.Published);
    }

    private void BuildControls()
    {
        _htmlBuilder
            .Replace("{{mediaView}}", _localizationSettings.MediaView)
            .Replace("{{closeMediaView}}", _localizationSettings.CloseMediaView)
            .Replace("{{scaleMediaView}}", _localizationSettings.ScaleMediaView)
            .Replace("{{fullScreenMediaView}}", _localizationSettings.FullScreenMediaView)
            .Replace("{{switchImageMediaView}}", _localizationSettings.SwitchImageMediaView);
    }

    private void BuildItems()
    {
        var itemsBuilder = new StringBuilder();
        var groupBuilder = new StringBuilder();
        var mediaFileGroup = CreateMediaFileGroup();
        foreach (var mediaFiles in mediaFileGroup)
        {
            foreach (var mediaFile in mediaFiles)
            {
                var mediaFilePath = $"{PhotoAlbum.FilesDirectoryName}/{mediaFile.Name}";
                if (mediaFile.IsImage)
                {
                    itemsBuilder.AppendLine(
                        _indexHtmlSettings.ImageItem.Replace("{{mediaFilePath}}",
                        mediaFilePath));
                }
                else
                {
                    itemsBuilder.AppendLine(
                        _indexHtmlSettings.VideoItem.Replace("{{mediaFilePath}}",
                        mediaFilePath));
                }
            }

            var mediaBlockBuilder = new StringBuilder();
            mediaBlockBuilder.AppendLine(
                _indexHtmlSettings.MediaBlock.Replace("{{mediaItems}}",
                itemsBuilder.ToString()));
            mediaBlockBuilder.AppendLine(_indexHtmlSettings.TextItem);
            groupBuilder.AppendLine(
                _indexHtmlSettings.GroupBlock.Replace("{{groupBlock}}",
                mediaBlockBuilder.ToString()));
            
            itemsBuilder.Clear();
        }
        
        _htmlBuilder.Replace("{{galleryItems}}", groupBuilder.ToString());
    }
    
    private IReadOnlyList<IReadOnlyList<MediaFile>> CreateMediaFileGroup()
    {
        return _mediaFiles
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
