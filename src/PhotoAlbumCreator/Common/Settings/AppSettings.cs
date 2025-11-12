using PhotoAlbumCreator.PhotoAlbums.Htmls;
using PhotoAlbumCreator.PhotoAlbums.Videos.Compressors.FFmpeg;

namespace PhotoAlbumCreator.Common.Settings;

public sealed record AppSettings(
    string CultureInfo,
    LocalizationSettings Localization,
    IndexHtmlSettings IndexHtml,
    FFmpegSettings FFmpeg);
