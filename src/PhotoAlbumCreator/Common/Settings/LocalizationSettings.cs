namespace PhotoAlbumCreator.Common.Settings;

public sealed record LocalizationSettings
{
    public string FileAdded { get; init; } = "FileAdded";

    public string FileFound { get; init; } = "FileFound";

    public string DirectoryNotFound { get; init; } = "DirectoryNotFound";

    public string RootPathInput { get; init; } = "RootPathInput";

    public string NoMediaInFileDirectory { get; init; } = "NoMediaInFileDirectory";

    public string AlbumAlreadyContainsItems { get; init; } = "AlbumAlreadyContainsItems";

    public string OperationCanceled { get; init; } = "OperationCanceled";

    public string GalleryUpdated { get; init; } = "GalleryUpdated";

    public string AlbumNameInput { get; init; } = "AlbumNameInput";

    public string AlbumNameEmptyInputError { get; init; } = "AlbumNameEmptyInputError";

    public string CommandInput { get; init; } = "CommandInput";

    public string CommandNotSpecified { get; init; } = "CommandNotSpecified";

    public string CommandNotRecognized { get; init; } = "CommandNotRecognized";

    public string CommandEmptyError { get; init; } = "CommandEmptyError";

    public string Error { get; init; } = "Error";

    public string Published { get; init; } = "Published";

    public string Processing { get; init; } = "Processing";

    public string Help { get; init; } = "Help";

    public string MediaView { get; init; } = "MediaView";

    public string CloseMediaView { get; init; } = "CloseMediaView";

    public string ScaleMediaView { get; init; } = "ScaleMediaView";

    public string FullScreenMediaView { get; init; } = "FullScreenMediaView";

    public string SwitchImageMediaView { get; init; } = "SwitchImageMediaView";

    public string PathInput { get; init; } = "PathInput";

    public string OnlyVideoFormatsSupports { get; init; } = "OnlyVideoFormatsSupports";
    
    public string PathNotFound { get; init; } = "PathNotFound";

    public string NoVideoInDirectory { get; init; } = "NoVideoInDirectory";

    public string CountOfVideoFilesFound { get; init; } = "CountOfVideoFilesFound";

    public string StartingCompression { get; init; } = "StartingCompression";

    public string VideoFilesCompressed { get; init; } = "VideoFilesCompressed";
}
