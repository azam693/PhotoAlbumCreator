namespace PhotoAlbumCreator.PhotoAlbums.Htmls;

public sealed class IndexHtmlSettings
{
    public int GroupTimeWindow { get; init; } = 2;

    public int CountElementsInGroup { get; init; } = 10;

    public string MediaFilesExistPattern { get; init; } = 
@"<article\s+class=""card""\s+[^>]*data-src=";

    public string GroupBlock { get; init; } =
@"<div class=""group"">
    {{groupBlock}}
</div>";

    public string MediaBlock { get; init; } =
@"<div class=""photos"">
    {{mediaItems}}
</div>";

    public string ImageItem { get; init; } =
@"<article class=""card"" data-type=""image"" data-src=""{{mediaFilePath}}"">
    <a class=""media"" href=""{{mediaFilePath}}"">
        <img src=""{{mediaFilePath}}"" loading=""lazy"" />
    </a>
</article>";

    public string VideoItem { get; init; } =
@"<article class=""card"" data-type=""video"" data-src=""{{mediaFilePath}}"">
    <a class=""media"" href=""{{mediaFilePath}}"">
        <video src=""{{mediaFilePath}}"" muted playsinline preload=""metadata""></video>
        <span class=""play"" aria-hidden=""true""></span>
		<span class=""video-ribbon"" aria-hidden=""true"">VIDEO</span>
    </a>
</article>";

    public string TextItem { get; init; } =
@"<!--
<div class=""story"">
    <p>Place text here.</p>
</div>
-->";

    public string AlbumBlock { get; init; } =
"<div class=\"photos photos--folders\">{{albumItems}}</div>";
    
    public string AlbumItem { get; init; } =
"<article class=\"card folder\"><a class=\"media\" href=\"{{albumPath}}\"><div class=\"folder-icon\" aria-hidden=\"true\"></div><div class=\"folder-text\"><div class=\"folder-name\">{{albumName}}</div></div></a></article>";
}
