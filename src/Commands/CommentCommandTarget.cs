using System;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class CommentCommandTarget : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        private IClassifier _classifier;
        private SnapshotSpan _selectedSpan;

        public CommentCommandTarget(IVsTextView adapter, IWpfTextView textView, IClassifierAggregatorService classifier)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.COMMENT_BLOCK)
        {
            _classifier = classifier.GetClassifier(textView.TextBuffer);
        }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            string selectedText = _selectedSpan.GetText();

            using (var edit = _view.TextBuffer.CreateEdit())
            {
                edit.Replace(_selectedSpan.Span, $"<!-- {selectedText} -->");
                edit.Apply();
            }

            return true;
        }

        protected override bool IsEnabled()
        {
            var start = _view.Selection.Start.Position.Position;
            var end = _view.Selection.End.Position.Position;
            var length = end - start;

            if (_view.Selection.IsEmpty)
            {
                var line = _view.Selection.Start.Position.GetContainingLine();
                start = line.Start;
                length = line.Length;
            }

            _selectedSpan = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, start, length);

            var spans = _classifier.GetClassificationSpans(_selectedSpan);
            var hasComments = spans.Any(s => s.ClassificationType.Classification.Contains(PredefinedClassificationTypeNames.Comment));

            return !hasComments;
        }
    }
}