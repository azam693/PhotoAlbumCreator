using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PhotoAlbumCreator.PhotoAlbums.Htmls;

public static class HtmlPrettifier
{
    // Список «пустых» (void) элементов HTML5, не влияющих на уровень отступа
    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "area","base","br","col","embed","hr","img","input","link","meta",
        "param","source","track","wbr"
    };

    // Токенайзер: комментарии/doctype, блоки script|style|pre|textarea, теги, текст
    private static readonly Regex Tokenizer = new(
        @"(?is)(
            <!--.*?-->                          # comment
          | <!DOCTYPE.*?>                      # doctype
          | <(script|style|pre|textarea)\b.*?>.*?</\1\s*>  # raw blocks
          | </?[^>]+?>                         # any tag
          | [^<]+                              # text
        )",
        RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

    public static string Format(string html, string indent = "  ")
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var sb = new StringBuilder(html.Length + html.Length / 8);
        int level = 0;

        foreach (Match m in Tokenizer.Matches(html))
        {
            string tok = m.Value;

            if (IsCommentOrDoctype(tok))
            {
                WriteLine(sb, level, tok.TrimEnd());
                continue;
            }

            if (IsRawBlock(tok)) // script/style/pre/textarea — как есть
            {
                WriteRawBlock(sb, level, tok, indent);
                continue;
            }

            if (IsTag(tok))
            {
                string tagName = GetTagName(tok);

                if (IsClosingTag(tok))
                {
                    level = Math.Max(0, level - 1);
                    WriteLine(sb, level, tok.Trim());
                }
                else if (IsSelfClosing(tok) || VoidTags.Contains(tagName))
                {
                    WriteLine(sb, level, tok.Trim());
                }
                else
                {
                    WriteLine(sb, level, tok.Trim());
                    level++; // увеличить после открывающего
                }
            }
            else
            {
                // Текст: если он «пустой»/пробельный — пропускаем лишнее
                var text = tok.Replace("\r\n", "\n");
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Нормализуем переносы внутри текста
                foreach (var line in text.Split('\n'))
                {
                    var t = line.TrimEnd();
                    if (t.Length == 0) continue;
                    WriteLine(sb, level, t);
                }
            }
        }

        return sb.ToString().Replace("\r\n", "\n"); // единый стиль перевода строк
    }

    private static void WriteLine(StringBuilder sb, int level, string content)
    {
        for (int i = 0; i < level; i++) sb.Append("  "); // по умолчанию 2 пробела; можно вынести параметром
        sb.AppendLine(content);
    }

    private static bool IsTag(string s) => s.Length > 1 && s[0] == '<' && s[^1] == '>';
    private static bool IsClosingTag(string s) => s.StartsWith("</");
    private static bool IsSelfClosing(string s) => s.EndsWith("/>") || s.EndsWith(" />");
    private static bool IsCommentOrDoctype(string s) =>
        s.StartsWith("<!--") || s.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
    private static bool IsRawBlock(string s) =>
        Regex.IsMatch(s, @"^(?is)<(script|style|pre|textarea)\b");

    private static string GetTagName(string tag)
    {
        // <tag ...> / </tag>
        var m = Regex.Match(tag, @"^</?\s*([a-zA-Z0-9:-]+)");
        return m.Success ? m.Groups[1].Value : "";
    }

    private static void WriteRawBlock(StringBuilder sb, int level, string block, string indent)
    {
        // Переносим блок целиком, но подвинем первую строку и внутренние строки
        var lines = block.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 1)
        {
            WriteLine(sb, level, lines[0]);
            return;
        }

        // первая строка
        WriteLine(sb, level, lines[0]);

        // середина — добавим +1 уровень отступа
        for (int i = 1; i < lines.Length - 1; i++)
        {
            var t = lines[i];
            for (int j = 0; j < level + 1; j++) sb.Append("  ");
            sb.AppendLine(t);
        }

        // последняя строка
        WriteLine(sb, level, lines[^1]);
    }
}
