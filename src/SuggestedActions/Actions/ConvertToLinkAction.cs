using System.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor
{
    class ConvertToLinkAction : BaseSuggestedAction
    {
        private SnapshotSpan _span;
        private ITextView _view;
        private const string _format = "[{0}](http://example.com)";

        public ConvertToLinkAction(SnapshotSpan span, ITextView view)
        {
            _span = span;
            _view = view;
        }

        public override string DisplayText
        {
            get { return "Convert To Link"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.Link; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            var spanText = _span.GetText();
            string text = string.Format(_format, spanText);

            using (var edit = _span.Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(_span, text);
                edit.Apply();
            }

            var start = _span.Start.Position + text.IndexOf('(') + 1;
            var end = _span.Start.Position + text.LastIndexOf(')');
            var length = end - start;

            var languageSpan = new SnapshotSpan(_span.Snapshot.TextBuffer.CurrentSnapshot, start, length);
            _view.Selection.Select(languageSpan, false);
            _view.Caret.MoveTo(languageSpan.Start);
        }
    }
}
