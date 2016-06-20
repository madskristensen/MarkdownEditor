using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class UncommentCommandTarget : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        private IClassifier _classifier;
        private IEnumerable<ClassificationSpan> _commentSpans;

        public UncommentCommandTarget(IVsTextView adapter, IWpfTextView textView, IClassifierAggregatorService classifier)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK)
        {
            _classifier = classifier.GetClassifier(textView.TextBuffer);
        }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            using (var edit = _view.TextBuffer.CreateEdit())
            {
                foreach (var span in _commentSpans)
                {
                    string text = span.Span.GetText()
                                            .Replace("<!--", string.Empty)
                                            .Replace("-->", string.Empty)
                                            .TrimStart()
                                            .TrimEnd(' ');

                    edit.Replace(span.Span, text);
                }

                edit.Apply();
            }

            return true;
        }

        protected override bool IsEnabled()
        {
            var start = _view.Selection.Start.Position.Position;
            var end = _view.Selection.End.Position.Position;
            var length = end - start;

            var selectedSpan = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, 0, _view.TextBuffer.CurrentSnapshot.Length);
            var spans = _classifier.GetClassificationSpans(selectedSpan);

            var allSComments = spans.Where(s => s.ClassificationType.Classification.Contains(PredefinedClassificationTypeNames.Comment));
            _commentSpans = allSComments.Where(c => c.Span.IntersectsWith(new Span(start, length)));

            return _commentSpans.Any();
        }
    }
}