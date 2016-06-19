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
    }
}
