using PhotoAlbumCreator.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PhotoAlbumCreator.PhotoAlbums.Htmls;

public class HtmlPrettifier
{
    private const string CRLF = "\r\n";
    private const string LF = "\n";

    // List of “void” HTML5 elements that do not affect indentation level
    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "area","base","br","col","embed","hr","img","input","link","meta",
        "param","source","track","wbr"
    };

    // Tokenizer: comment/doctype tokens, script | style | pre | textarea content blocks, tag tokens, text tokens
    private static readonly Regex Tokenizer = new(
        @"(?is)(
            <!--.*?-->                                     # comment
          | <!DOCTYPE.*?>                                  # doctype
          | <(script|style|pre|textarea)\b.*?>.*?</\1\s*>  # raw blocks
          | </?[^>]+?>                                     # any tag
          | [^<]+                                          # text
        )",
        RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

    private readonly string _indent;

    public HtmlPrettifier(int indentCount = 2)
    {
        if (indentCount < 0)
        {
            throw new AlbumException("Indent count cannot be negative.");
        }

        _indent = new string(' ', indentCount);
    }

    public string Format(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        int level = 0;
        var htmlBuilder = new StringBuilder(html.Length + html.Length / 8);

        foreach (Match matched in Tokenizer.Matches(html))
        {
            var token = matched.Value;

            if (IsCommentOrDoctype(token))
            {
                WriteLine(htmlBuilder, level, token.TrimEnd());
                continue;
            }

            // script/style/pre/textarea — as it is
            if (IsRawBlock(token))
            {
                WriteRawBlock(htmlBuilder, level, token);
                continue;
            }

            if (IsTag(token))
            {
                var tagName = GetTagName(token);
                if (IsClosingTag(token))
                {
                    level = Math.Max(0, level - 1);
                    WriteLine(htmlBuilder, level, token.Trim());
                }
                else if (IsSelfClosing(token) || VoidTags.Contains(tagName))
                {
                    WriteLine(htmlBuilder, level, token.Trim());
                }
                else
                {
                    WriteLine(htmlBuilder, level, token.Trim());
                    // Increase after open tag
                    level++;
                }
            }
            else
            {
                // Text: if it is “empty”/whitespace — skip the excess
                var text = token.Replace(CRLF, LF);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                // Normalize line breaks inside text
                foreach (var line in text.Split(LF))
                {
                    var t = line.TrimEnd();
                    if (t.Length == 0)
                        continue;

                    WriteLine(htmlBuilder, level, t);
                }
            }
        }

        return htmlBuilder
            .ToString()
            .Replace(CRLF, LF);
    }

    private void WriteLine(StringBuilder htmlBuilder, int level, string content)
    {
        // 2 spaces by default; can be exposed as a parameter
        for (int i = 0; i < level; i++)
        {
            htmlBuilder.Append(_indent);
        }

        htmlBuilder.AppendLine(content);
    }

    private void WriteRawBlock(StringBuilder htmlBuilder, int level, string block)
    {
        // Move the entire block, but shift the first line and the inner lines
        var lines = block
            .Replace(CRLF, LF)
            .Split(LF);
        if (lines.Length == 1)
        {
            WriteLine(htmlBuilder, level, lines[0]);
            return;
        }

        // First line
        WriteLine(htmlBuilder, level, lines[0]);

        // In the middle — add +1 indentation level
        for (int i = 1; i < lines.Length - 1; i++)
        {
            var text = lines[i];
            for (int j = 0; j < level + 1; j++)
            {
                htmlBuilder.Append(_indent);
            }

            htmlBuilder.AppendLine(text);
        }

        WriteLine(htmlBuilder, level, lines[^1]);
    }

    private static bool IsTag(string token)
    {
        return token.Length > 1 && token[0] == '<' && token[^1] == '>';
    }

    private static bool IsClosingTag(string token)
    {
        return token.StartsWith("</");
    }

    private static bool IsSelfClosing(string token)
    {
        return token.EndsWith("/>") || token.EndsWith(" />");
    }

    private static bool IsCommentOrDoctype(string token)
    {
        return token.StartsWith("<!--")
            || token.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRawBlock(string s)
    {
        return Regex.IsMatch(s, @"^(?is)<(script|style|pre|textarea)\b");
    }

    private static string GetTagName(string tag)
    {
        // <tag ...> / </tag>
        var matchedTag = Regex.Match(tag, @"^</?\s*([a-zA-Z0-9:-]+)");

        return matchedTag.Success
            ? matchedTag.Groups[1].Value
            : string.Empty;
    }
}
