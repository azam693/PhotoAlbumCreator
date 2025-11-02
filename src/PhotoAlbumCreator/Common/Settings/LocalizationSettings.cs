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

    public string Help { get; init; } = "Help";
}
