using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Markdig.Parsers;
using Markdig.Syntax;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Markdig.Syntax.Inlines;
using Markdig;
using Microsoft.VisualStudio.Text.Tagging;
using System.Windows.Controls;

namespace MarkdownEditor
{
    internal class MarkdownClassifier : IClassifier, ITagger<IOutliningRegionTag>
    {
        private readonly IClassificationType _code, _header, _quote, _bold, _italic, _link, _html, _comment;
        private readonly ITextBuffer _buffer;
        private MarkdownDocument _doc;
        private bool _isProcessing;
        private readonly MarkdownPipeline _pipeline;

        internal MarkdownClassifier(ITextBuffer buffer, IClassificationTypeRegistryService registry)
        {
            _buffer = buffer;
            _code = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownCode);
            _header = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownHeader);
            _quote = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownQuote);
            _bold = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownBold);
            _italic = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownItalic);
            _link = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
            _html = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownHtml);
            _comment = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);

            var pipelineBuilder = new MarkdownPipelineBuilder().UsePreciseSourceLocation();
            _pipeline = pipelineBuilder.Build();

            ParseDocument();

            _buffer.Changed += bufferChanged;
        }

        private void bufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ParseDocument();
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var list = new List<ClassificationSpan>();

            if (_doc == null || _isProcessing || span.IsEmpty)
                return list;

            var e = _doc.Descendants();

            foreach (var mdobj in e)
            {
                // Break the loop if the object spans are greater than the snapshotspan
                if (mdobj.Span.Start > span.End.Position)
                    break;

                var blockSpan = new Span(mdobj.Span.Start, mdobj.Span.Length);

                try
                {
                    if (span.IntersectsWith(blockSpan))
                    {
                        var all = GetClassificationTypes(mdobj, span);

                        foreach (var range in all.Keys)
                        {
                            var snapspan = new SnapshotSpan(span.Snapshot, range);
                            list.Add(new ClassificationSpan(snapspan, all[range]));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // For some reason span.IntersectsWith throws in some cases.
                    System.Diagnostics.Debug.Write(ex);
                }
            }

            return list;
        }

        private Dictionary<Span, IClassificationType> GetClassificationTypes(MarkdownObject mdobj, SnapshotSpan span)
        {
            var spans = new Dictionary<Span, IClassificationType>();

            if (mdobj is HeadingBlock)
            {
                spans.Add(mdobj.ToSimpleSpan(), _header);
            }
            else if (mdobj is CodeBlock)
            {
                spans.Add(mdobj.ToSimpleSpan(), _code);
            }
            else if (mdobj is QuoteBlock)
            {
                spans.Add(mdobj.ToSimpleSpan(), _quote);
            }
            else if (mdobj is LinkInline)
            {
                spans.Add(mdobj.ToSimpleSpan(), _link);
            }
            else if (mdobj is EmphasisInline)
            {
                var emphasis = (EmphasisInline)mdobj;
                var type = emphasis.IsDouble ? _bold : _italic;
                spans.Add(mdobj.ToSimpleSpan(), type);
            }
            else if (mdobj is HtmlBlock || mdobj is HtmlInline || mdobj is HtmlEntityInline)
            {
                var block = mdobj as HtmlBlock;

                if (block != null && block.Type == HtmlBlockType.Comment)
                    spans.Add(mdobj.ToSimpleSpan(), _comment);
                else
                    spans.Add(mdobj.ToSimpleSpan(), _html);
            }

            return spans;
        }

        private async void ParseDocument()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            await Task.Run(() =>
            {
                var rawText = _buffer.CurrentSnapshot.GetText();
                _doc = MarkdownParser.Parse(rawText, _pipeline);

                SnapshotSpan span = new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);

                ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(span));
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));

                _isProcessing = false;
            });
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || _doc == null || !MarkdownEditorPackage.Options.EnableOutlining)
                yield break;

            var descendants = _doc.Descendants();
            var snapshot = spans.First().Snapshot;

            // Code blocks
            var codeBlocks = descendants.OfType<FencedCodeBlock>();

            foreach (var block in codeBlocks)
            {
                string text = $"{block.Info.ToUpperInvariant()} Code Block".Trim();
                string tooltip = new string(block.Lines.ToString().Take(800).ToArray());

                var span = new SnapshotSpan(snapshot, block.ToSimpleSpan());
                var tag = new OutliningRegionTag(false, false, text, tooltip);

                yield return new TagSpan<IOutliningRegionTag>(span, tag);
            }

            // HTML Blocks
            var htmlBlocks = descendants.OfType<HtmlBlock>();

            foreach (var block in htmlBlocks)
            {
                // This prevents outlining for single line comments
                if (block.Lines.Count == 1)
                    continue;

                string text = "HTML Block";
                string tooltip = new string(block.Lines.ToString().Take(800).ToArray());

                var span = new SnapshotSpan(snapshot, block.ToSimpleSpan());
                var tag = new OutliningRegionTag(false, false, text, tooltip);

                yield return new TagSpan<IOutliningRegionTag>(span, tag);
            }
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
