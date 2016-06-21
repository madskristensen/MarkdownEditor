using System.Runtime.CompilerServices;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Microsoft.VisualStudio.Text;

namespace MarkdownEditor.Parsing
{
    public static class MarkdownFactory
    {
        private static readonly ConditionalWeakTable<ITextSnapshot, MarkdownDocument> CachedDocuments = new ConditionalWeakTable<ITextSnapshot, MarkdownDocument>();

        static MarkdownFactory()
        {
            Pipeline = new MarkdownPipelineBuilder()
                .UsePragmaLines()
                .UseAdvancedExtensions().Build();
        }

        public static MarkdownPipeline Pipeline { get; }


        public static MarkdownDocument ParseToMarkdown(this ITextSnapshot snapshot)
        {
            return CachedDocuments.GetValue(snapshot, key =>
            {
                var text = key.GetText();
                var markdownDocument =  Markdown.Parse(text, Pipeline);
                return markdownDocument;
            });
        }
    }
}