using PhotoAlbumCreator.Common.Settings;
using System;
using System.Text;

namespace PhotoAlbumCreator.PhotoAlbums.Htmls;

public sealed class IndexHtmlTemplate
{
    private readonly LocalizationSettings _localizationSettings;

    private StringBuilder _htmlBuilder;

    public IndexHtmlTemplate(string html, LocalizationSettings localizationSettings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);
        ArgumentNullException.ThrowIfNull(localizationSettings);

        _htmlBuilder = new StringBuilder(html);
        _localizationSettings = localizationSettings;
    }

    public string BuildHtml()
    {
        return _htmlBuilder.ToString();
    }

    public IndexHtmlTemplate BuildHeader(string albumeName, MediaFile mediaFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumeName);
        ArgumentNullException.ThrowIfNull(mediaFile);

        return this
            .SetTitle(albumeName)
            .SetCreatedAt(
                mediaFile.GetCreatedAtISO(),
                mediaFile.GetCreatedAtHuman())
            .SetPublishedText(_localizationSettings.Published);
    }

    public IndexHtmlTemplate BuildControls()
    {
        return this
            .SetMediaViewText(_localizationSettings.MediaView)
            .SetCloseMediaViewText(_localizationSettings.CloseMediaView)
            .SetScaleMediaViewText(_localizationSettings.ScaleMediaView)
            .SetFullScreenMediaViewText(_localizationSettings.FullScreenMediaView)
            .SetSwitchImageMediaViewText(_localizationSettings.SwitchImageMediaView);
    }

    public IndexHtmlTemplate SetTitle(string title)
    {
        return ReplaceTemplate("{{title}}", title);
    }

    public IndexHtmlTemplate SetCreatedAt(string createdAtIso, string createdAtHuman)
    {
        return ReplaceTemplate("{{createdAtIso}}", createdAtIso)
            .ReplaceTemplate("{{createdAtHuman}}", createdAtHuman);
    }

    public IndexHtmlTemplate SetPublishedText(string publishedText)
    {
        return ReplaceTemplate("{{published}}", publishedText);
    }

    public IndexHtmlTemplate SetMediaViewText(string mediaViewLabel)
    {
        return ReplaceTemplate("{{mediaView}}", mediaViewLabel);
    }
    
    public IndexHtmlTemplate SetCloseMediaViewText(string closeMediaView)
    {
        return ReplaceTemplate("{{closeMediaView}}", closeMediaView);
    }
    
    public IndexHtmlTemplate SetScaleMediaViewText(string scaleMediaView)
    {
        return ReplaceTemplate("{{scaleMediaView}}", scaleMediaView);
    }

    public IndexHtmlTemplate SetFullScreenMediaViewText(string fullScreenMediaView)
    {
        return ReplaceTemplate("{{fullScreenMediaView}}", fullScreenMediaView);
    }

    public IndexHtmlTemplate SetSwitchImageMediaViewText(string switchImageMediaView)
    {
        return ReplaceTemplate("{{switchImageMediaView}}", switchImageMediaView);
    }

    private IndexHtmlTemplate ReplaceTemplate(string template, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        _htmlBuilder.Replace(template, text);

        return this;
    }
}
