namespace PhotoAlbumCreator.PhotoAlbums.Videos.Compressors;

public interface ICompressor
{
    bool Compress(string inputPath, string outputPath, VideoQualities quality);
}
