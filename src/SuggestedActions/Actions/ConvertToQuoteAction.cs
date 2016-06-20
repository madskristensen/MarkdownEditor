using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor
{
    class ConvertToQuoteAction : BaseSuggestedAction
    {
        private SnapshotSpan _span;
        private ITextView _view;

        public ConvertToQuoteAction(SnapshotSpan span, ITextView view)
        {
            _span = span;
            _view = view;
        }

        public override string DisplayText
        {
            get { return "Convert To Blockqoute"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.Quote; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            var startLine = _span.Start.GetContainingLine();
            var endLine = _span.End.GetContainingLine();
            var lines = _span.Snapshot.Lines.Where(l => l.LineNumber >= startLine.LineNumber && l.LineNumber <= endLine.LineNumber);

            _view.Caret.MoveTo(endLine.End);

            using (var edit = _span.Snapshot.TextBuffer.CreateEdit())
            {
                foreach (var line in lines)
                {
                    edit.Insert(line.Start.Position, "> ");
                }

                edit.Apply();
            }

            _view.Selection.Clear();
        }
    }
}
