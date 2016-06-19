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

namespace MarkdownEditor
{
    internal class MarkdownClassifier : IClassifier
    {
        private readonly IClassificationType _code, _header, _quote, _bold, _italic, _link, _html;
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

            if (_doc == null || _isProcessing)
                return list;

            var e = _doc.Descendants();

            foreach (var mdobj in e)
            {
                // Break the loop if the object spans are greater than the snapshotspan
                if (mdobj.Span.Start > span.End.Position)
                    break;

                var blockSpan = new Span(mdobj.Span.Start, mdobj.Span.Length);

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

                _isProcessing = false;
            });
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
    }
}
