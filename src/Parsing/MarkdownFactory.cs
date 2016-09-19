using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Markdig;
using Markdig.Extensions.Footers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor.Parsing
{
    public static class MarkdownFactory
    {
        public static object _syncRoot = new object();
        private static readonly ConditionalWeakTable<ITextSnapshot, MarkdownDocument> CachedDocuments = new ConditionalWeakTable<ITextSnapshot, MarkdownDocument>();

        static MarkdownFactory()
        {
            Pipeline = new MarkdownPipelineBuilder()
                .UsePragmaLines()
                .UseDiagrams()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter()
                .Build();
        }

        public static MarkdownPipeline Pipeline { get; }

        public static MarkdownDocument ParseToMarkdown(this ITextSnapshot snapshot, string file = null)
        {
            lock (_syncRoot)
            {
                return CachedDocuments.GetValue(snapshot, key =>
                {
                    var text = key.GetText();
                    var markdownDocument = Markdown.Parse(text, Pipeline);
                    Parsed?.Invoke(snapshot, new ParsingEventArgs(markdownDocument, file, snapshot));
                    return markdownDocument;
                });
            }
        }

        public static event EventHandler<ParsingEventArgs> Parsed;

        public static IEnumerable<Error> Validate(this MarkdownDocument doc, string file)
        {
            var descendants = doc.Descendants().OfType<LinkInline>();

            foreach (var link in descendants)
            {
                if (!IsUrlValid(file, link.Url))
                    yield return new Error
                    {
                        File = file,
                        Message = $"The file \"{link.Url}\" could not be resolved.",
                        Line = link.Line,
                        Column = link.Column,
                        ErrorCode = "missing-file",
                        // FIX: There seems to be something wrong with the Markdig parser
                        //      when parsing a referenced image e.g.
                        //      ![The image][image]
                        //      ^^^^^^^^^^^^^~~~~~^
                        //      [image]: images/the-image.png
                        //
                        //      The intension of the span is to add error curlies underneat image only
                        //      which is shown with the tilde above. But the fallback option adds them
                        //      to the entire span, ^ and ~.
                        Span = new Span(
                            link.UrlSpan?.Start ?? link.Span.Start,
                            link.UrlSpan?.Length ?? link.Span.Length)
                    };
            }
        }

        private static bool IsUrlValid(string file, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (url.Contains("://") || url.StartsWith("/") || url.StartsWith("#") || url.StartsWith("mailto:"))
                return true;

            if (url.Contains('\\'))
                return false;

            var query = url.IndexOf('?');
            if (query > -1)
                url = url.Substring(0, query);

            var fragment = url.IndexOf('#');
            if (fragment > -1)
                url = url.Substring(0, fragment);

            try
            {
                string currentDir = Path.GetDirectoryName(file);
                string path = Path.Combine(currentDir, url);

                if(File.Exists(path) || (String.IsNullOrWhiteSpace(Path.GetExtension(path)) &&
                  ContentTypeDefinition.MarkdownExtensions.Any(ext => File.Exists(path + ext))))
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return true;
            }
        }

        public static bool MatchSmartBlock(string text)
        {
            MarkdownDocument doc;
            return MatchSmartBlock(text, out doc);
        }

        public static bool MatchSmartBlock(string text, out MarkdownDocument doc)
        {
            // Temp workaround for list items: We trim the beginning of the line to be able to parse nested list items.
            text = text.TrimStart();
            doc = Markdown.Parse(text, Pipeline);
            return doc.Count != 0 && (doc[0] is QuoteBlock || doc[0] is ListBlock || (doc[0] is CodeBlock && !(doc[0] is FencedCodeBlock)) || doc[0] is FooterBlock);
        }

        public static bool TryParsePendingSmartBlock(this ITextView view, out List<Block> blocks, out MarkdownDocument doc)
        {
            // Prematch the current line to detect a smart block
            var extend = view.Caret.ContainingTextViewLine.Extent;
            var text = extend.GetText();
            blocks = null;
            doc = null;

            if (!MatchSmartBlock(text))
            {
                return false;
            }

            // Parse only until the end of the line after the caret
            // Because most of the time, a user would have typed characters before typing return
            // it is not efficient to re-use cached MarkdownDocument from Markdownfactory, as it may be invalid,
            // and then after the hit return, the browser would have to be updated with a new snapshot
            var snapshot = view.TextBuffer.CurrentSnapshot;
            var textFromTop = snapshot.GetText(0, view.Caret.ContainingTextViewLine.Extent.Span.End);
            doc = Markdown.Parse(textFromTop, Pipeline);
            var caretPosition = view.Caret.Position.BufferPosition.Position;
            var lastChild = doc.FindBlockAtPosition(caretPosition);
            if (lastChild == null || !lastChild.ContainsPosition(caretPosition))
            {
                return false;
            }

            // Re-build list of blocks
            blocks = new List<Block>();
            var block = lastChild;
            while (block != null)
            {
                // We don't add ListBlock (as they should have list items)
                if (block != doc && !(block is ListBlock))
                {
                    blocks.Add(block);
                }
                block = block.Parent;
            }
            blocks.Reverse();

            return blocks.Count > 0;
        }
    }
}
