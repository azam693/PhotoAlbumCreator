namespace PhotoAlbumCreator.PhotoAlbums.Videos.Compressors.FFmpeg;

public sealed record FFmpegSettings(string Path, string Arguments, FFmpegAudioSettings Audio);

public sealed record FFmpegAudioSettings(bool Copy, string Codec, int BitrateKbps)
{
    public static FFmpegAudioSettings CreateDefault()
    {
        return new FFmpegAudioSettings(
            Copy: false,
            Codec: "aac",
            BitrateKbps: 192);
    }
}
