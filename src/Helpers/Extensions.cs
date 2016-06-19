using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Markdig.Syntax;
using Microsoft.VisualStudio.Text;

namespace MarkdownEditor
{
    public static class Extensions
    {
        public static Span ToSimpleSpan(this MarkdownObject block)
        {
            return new Span(block.Span.Start, block.Span.Length);
        }

        public static string ToFriendlyName(this string fileName)
        {
            var text = Path.GetFileNameWithoutExtension(fileName)
                            .Replace("-", " ")
                            .Replace("_", " ");

            text = Regex.Replace(text, "(\\B[A-Z])", " $1");

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }
    }
}
