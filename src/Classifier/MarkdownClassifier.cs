using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarkdownEditor
{
    internal class MarkdownClassifier : IClassifier
    {
        private readonly IClassificationType _code, _header, _quote, _bold, _italic, _link, _html, _comment,
            _naturalLanguage;
        private readonly ITextBuffer _buffer;
        private MarkdownDocument _doc;
        private bool _isProcessing;
        private string _file;

        internal MarkdownClassifier(ITextBuffer buffer, IClassificationTypeRegistryService registry, string file)
        {
            _buffer = buffer;
            _file = file;
            _code = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownCode);
            _header = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownHeader);
            _quote = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownQuote);
            _bold = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownBold);
            _italic = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownItalic);
            _link = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownLink);
            _html = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownHtml);
            _comment = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownComment);
            _naturalLanguage = registry.GetClassificationType(MarkdownClassificationTypes.MarkdownNaturalLanguage);

            ParseDocument();

            _buffer.Changed += bufferChanged;
        }

        private void bufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ParseDocument();
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            Span lastSpan = new Span();
            var list = new List<ClassificationSpan>();

            if (_doc == null || _isProcessing || span.IsEmpty)
                return list;

            var e = _doc.Descendants().Where(b => b.Span.Start <= span.End && b.Span.Length > 0);

            foreach (var mdobj in e)
            {
                var blockSpan = new Span(mdobj.Span.Start, mdobj.Span.Length);

                try
                {
                    if (span.IntersectsWith(blockSpan))
                    {
                        var all = GetClassificationTypes(mdobj, span);

                        foreach (var range in all.Keys)
                        {
                            var snapspan = new SnapshotSpan(span.Snapshot, range);

                            // Literal spans may appear as children of other spans such as headings.  When this
                            // occurs, we ignore them.
                            //if (!lastSpan.Contains(snapspan))
                            //{
                            list.Add(new ClassificationSpan(snapspan, all[range]));
                            lastSpan = snapspan;
                            //}
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
            else if (MarkdownEditorPackage.Options.CodeSystemFont && (mdobj is CodeBlock || mdobj is CodeInline))
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
            else if (mdobj is LiteralInline)
            {
                spans.Add(mdobj.ToSimpleSpan(), _naturalLanguage);
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
                _doc = _buffer.CurrentSnapshot.ParseToMarkdown(_file);

                SnapshotSpan span = new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);

                ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(span));

                _isProcessing = false;
            });
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
    }
}
