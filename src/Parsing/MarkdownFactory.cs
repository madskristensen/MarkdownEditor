using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Markdig;
using Markdig.Extensions.Footers;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor.Parsing
{
    public static class MarkdownFactory
    {
        private const string AttachedExceptionKey = "attached-exception";
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

            // Use a minimalistic pipeline if we get an error while parsing with all extensions active
            PipelineSafer = new MarkdownPipelineBuilder()
                .UsePragmaLines()
                .Build();
        }

        public static MarkdownPipeline Pipeline { get; }

        public static MarkdownPipeline PipelineSafer { get; }

        public static Exception GetAttachedException(this MarkdownDocument markdownDocument)
        {
            return markdownDocument.GetData(AttachedExceptionKey) as Exception;
        }

        public static MarkdownDocument ParseToMarkdown(this ITextSnapshot snapshot, string file = null)
        {
            lock (_syncRoot)
            {
                return CachedDocuments.GetValue(snapshot, key =>
                {
                    var text = key.GetText();
                    var markdownDocument = ParseToMarkdown(text);
                    Parsed?.Invoke(snapshot, new ParsingEventArgs(markdownDocument, file, snapshot));
                    return markdownDocument;
                });
            }
        }

        public static MarkdownDocument ParseToMarkdown(string text, MarkdownPipeline pipeline = null)
        {
            // Safe version that will always return a MarkdownDocument even if there is an exception while parsing
            MarkdownDocument markdownDocument;

            pipeline = pipeline ?? Pipeline;

            // Try first to parse a document with all exceptions active
            try
            {
                markdownDocument = Markdown.Parse(text, pipeline);
            }
            catch (Exception ex)
            {
                // If we have an error, try to parse with a safer/simpler pipeline
                try
                {
                    markdownDocument = Markdown.Parse(text, PipelineSafer);
                }
                catch (Exception)
                {
                    // with have a serious error even when parsing with safer pipeline
                    // return an empty document and log an error
                    markdownDocument = new MarkdownDocument();
                }
                markdownDocument.Span = new SourceSpan(0, text.Length - 1);

                // we attach the exception to the document that will be later displayed to the user
                markdownDocument.SetData(AttachedExceptionKey, ex);
            }
            return markdownDocument;
        }

        public static event EventHandler<ParsingEventArgs> Parsed;

        public static IEnumerable<Error> Validate(this MarkdownDocument doc, string file)
        {
            var descendants = doc.Descendants().OfType<LinkInline>();

            var exception = doc.GetAttachedException();
            if (exception != null)
            {
                yield return new Error
                {
                    File = file,
                    Message = "Unexpected error occured while parsing. Please log an issue to https://github.com/madskristensen/MarkdownEditor/issues Reason: " + exception,
                    Line = 0,
                    Column = 0,
                    ErrorCode = "MK0000",
                    Fatal = true,
                    Span = new Span(doc.Span.Start, doc.Span.Length)
                };
            }

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
                        //      The link.Reference.UrlSpan doesn't have correct values
                        //      which forces us to use this code
                        Span = link.Reference == null ? new Span(link.UrlSpan.Value.Start, link.UrlSpan.Value.Length) : new Span(link.Span.Start, link.Span.Length)
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

                if (File.Exists(path) || (String.IsNullOrWhiteSpace(Path.GetExtension(path)) &&
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
            doc = ParseToMarkdown(text);
            return doc.Count != 0 && (doc[0] is QuoteBlock || doc[0] is ListBlock || (doc[0] is CodeBlock && !(doc[0] is FencedCodeBlock)) || doc[0] is FooterBlock);
        }

        public static bool TryParsePendingSmartBlock(this ITextView view, bool fullLine, out List<Block> blocks, out MarkdownDocument doc, out bool isEmptyLineText, out bool isEmptyLineAfterCaret)
        {
            // Prematch the current line to detect a smart block
            var caretPosition = view.Caret.Position.BufferPosition.Position;
            var startLinePosition = view.Caret.ContainingTextViewLine.Start.Position;
            var endLinePosition = fullLine
                ? view.Caret.ContainingTextViewLine.EndIncludingLineBreak.Position
                : caretPosition;
            var text = view.TextBuffer.CurrentSnapshot.GetText(startLinePosition, endLinePosition - startLinePosition);
            blocks = null;
            doc = null;
            isEmptyLineText = false;
            isEmptyLineAfterCaret = false;

            MarkdownDocument singleLineDoc;
            if (!MatchSmartBlock(text, out singleLineDoc))
            {
                return false;
            }

            // Detect any literal text inside the line we parsed
            var allLiteralsEmpty =
                singleLineDoc.Descendants()
                    .OfType<LiteralInline>()
                    .All(literal => literal.Content.IsEmptyOrWhitespace());

            // Detect any non-whitespace chars after the caret to the end of the line
            var textLine = view.TextBuffer.CurrentSnapshot.GetText(caretPosition, view.Caret.ContainingTextViewLine.End.Position - caretPosition);
            isEmptyLineAfterCaret = true;
            for (int i = 0; i < textLine.Length; i++)
            {
                if (!char.IsWhiteSpace(textLine[i]))
                {
                    isEmptyLineAfterCaret = false;
                    break;
                }
            }

            isEmptyLineText = allLiteralsEmpty && isEmptyLineAfterCaret;

            // Parse only until the end of the line after the caret
            // Because most of the time, a user would have typed characters before typing return
            // it is not efficient to re-use cached MarkdownDocument from Markdownfactory, as it may be invalid,
            // and then after the hit return, the browser would have to be updated with a new snapshot
            var snapshot = view.TextBuffer.CurrentSnapshot;
            var textFromTop = snapshot.GetText(0, endLinePosition);
            doc = ParseToMarkdown(textFromTop);
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
