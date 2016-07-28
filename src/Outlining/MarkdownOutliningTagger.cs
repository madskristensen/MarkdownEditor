using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Markdig.Syntax;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace MarkdownEditor.Outlining
{
    public class MarkdownOutliningTagger : ITagger<IOutliningRegionTag>
    {
        private MarkdownDocument _doc;
        private bool _isProcessing;
        private ITextBuffer _buffer;
        private string _file;

        public MarkdownOutliningTagger(ITextBuffer buffer, string file)
        {
            _buffer = buffer;
            _file = file;
            ParseDocument();

            _buffer.Changed += bufferChanged;
        }

        private void bufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ParseDocument();
        }

        private async void ParseDocument()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            await Task.Run(() =>
            {
                _doc = _buffer.CurrentSnapshot.ParseToMarkdown(_file);

                SnapshotSpan span = new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
                _isProcessing = false;

                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
            });
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || _doc == null || !MarkdownEditorPackage.Options.EnableOutlining)
                return Enumerable.Empty<ITagSpan<IOutliningRegionTag>>();

            var descendants = _doc.Descendants();
            var snapshot = spans.First().Snapshot;

            var codeBlocks = ProcessCodeBlocks(descendants, snapshot);
            var htmlBlocks = ProcessHtmlBlocks(descendants, snapshot);
            var headingBlocks = ProcessHeadingBlocks(descendants, snapshot);

            return codeBlocks.Union(htmlBlocks).Union(headingBlocks);
        }

        private IEnumerable<ITagSpan<IOutliningRegionTag>> ProcessCodeBlocks(IEnumerable<MarkdownObject> descendants, ITextSnapshot snapshot)
        {
            var codeBlocks = descendants.OfType<FencedCodeBlock>();

            foreach (var block in codeBlocks)
            {
                if (block.IsOpen || block.Lines.Count == 0)
                    continue;

                string text = $"{block.Info.ToUpperInvariant()} Code Block".Trim();
                string tooltip = new string(block.Lines.ToString().Take(800).ToArray());

                var span = new SnapshotSpan(snapshot, block.ToSimpleSpan());
                yield return CreateTag(span, text, tooltip);
            }
        }

        private IEnumerable<ITagSpan<IOutliningRegionTag>> ProcessHtmlBlocks(IEnumerable<MarkdownObject> descendants, ITextSnapshot snapshot)
        {
            var htmlBlocks = descendants.OfType<HtmlBlock>();

            foreach (var block in htmlBlocks)
            {
                // This prevents outlining for single line comments
                if (block.Lines.Count == 1)
                    continue;

                string text = "HTML Block";
                string tooltip = new string(block.Lines.ToString().Take(800).ToArray());

                var span = new SnapshotSpan(snapshot, block.ToSimpleSpan());
                yield return CreateTag(span, text, tooltip);
            }
        }

        private IEnumerable<ITagSpan<IOutliningRegionTag>> ProcessHeadingBlocks(IEnumerable<MarkdownObject> descendants, ITextSnapshot snapshot)
        {
            var headingBlocks = descendants.OfType<HeadingBlock>();

            foreach (var block in headingBlocks)
            {
                var next = headingBlocks.FirstOrDefault(h => h.Level <= block.Level && h.Line > block.Line);
                var length = 0;

                if (next != null)
                    length = GetSectionEnding(snapshot.GetLineFromLineNumber(next.Line - 1)) - block.Span.Start;
                else
                    length = GetSectionEnding(snapshot.GetLineFromLineNumber(snapshot.LineCount - 1)) - block.Span.Start;

                string text = snapshot.GetText(block.ToSimpleSpan());
                var span = new SnapshotSpan(snapshot, block.Span.Start, length);

                yield return CreateTag(span, text, span.GetText());
            }
        }

        private int GetSectionEnding(ITextSnapshotLine line)
        {
            while (line.Extent.IsEmpty && line.LineNumber > 0)
            {
                line = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
            }

            return line.End;
        }

        private static TagSpan<IOutliningRegionTag> CreateTag(SnapshotSpan span, string text, string tooltip = null)
        {
            var tag = new OutliningRegionTag(false, false, text, tooltip);
            return new TagSpan<IOutliningRegionTag>(span, tag);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
