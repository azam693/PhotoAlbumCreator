using PhotoAlbumCreator.PhotoAlbums.Htmls;

namespace PhotoAlbumCreator.Common.Settings;

public sealed record AppSettings(
    string CultureInfo,
    LocalizationSettings Localization,
    IndexHtmlSettings IndexHtml);
