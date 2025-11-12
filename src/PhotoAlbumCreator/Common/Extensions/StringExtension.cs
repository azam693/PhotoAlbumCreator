namespace PhotoAlbumCreator.Common.Extensions;

public static class StringExtension
{
    public static string Fmt(this string format, params object[] args)
    {
        return string.Format(format, args);
    }
}
