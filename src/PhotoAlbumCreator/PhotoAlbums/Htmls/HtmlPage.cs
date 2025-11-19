using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.IO;

namespace PhotoAlbumCreator.PhotoAlbums.Htmls;

public sealed class HtmlPage
{
    private readonly IHtmlDocument _document;

    public HtmlPage(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var parser = new HtmlParser();
        _document = parser.ParseDocument(html);
    }

    public string BuildHtml()
    {
        using var stringWriter = new StringWriter();

        _document.ToHtml(stringWriter, new PrettyMarkupFormatter());

        return stringWriter.ToString();
    }

    public bool HasValue(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        var element = GetElement(selector);

        return element != null && !string.IsNullOrWhiteSpace(element.TextContent);
    }

    public IElement? GetElement(string selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        return _document.QuerySelector(selector);
    }

    public bool AppendChild(string selector, string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        ArgumentNullException.ThrowIfNull(html);
        var element = GetElement(selector);
        if (element is null)
            return false;

        AppendChild(element, html);

        return true;
    }
    
    public void AppendChild(IElement element, string html)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(html);

        element.Insert(AdjacentPosition.BeforeEnd, html);
    }
}
