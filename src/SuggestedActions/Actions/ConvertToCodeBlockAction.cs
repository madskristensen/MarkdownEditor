using System;
using System.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor
{
    class ConvertToCodeBlockAction : BaseSuggestedAction
    {
        private SnapshotSpan _span;
        private ITextView _view;
        private const string _language = "<language>";

        public ConvertToCodeBlockAction(SnapshotSpan span, ITextView view)
        {
            _span = span;
            _view = view;
        }

        public override string DisplayText
        {
            get { return "Convert To Code Block"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.Code; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            var startLine = _span.Start.GetContainingLine();
            var endLine = _span.End.GetContainingLine();
            var startPosition = startLine.Start.Position;

            var span = new SnapshotSpan(startLine.Start, endLine.End);
            string text = span.GetText();

            using (var edit = span.Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(span, $"```{_language}{Environment.NewLine}{text}{Environment.NewLine}```");
                edit.Apply();
            }

            var languageSpan = new SnapshotSpan(span.Snapshot.TextBuffer.CurrentSnapshot, startPosition + 3, _language.Length);
            _view.Selection.Select(languageSpan, false);
            _view.Caret.MoveTo(languageSpan.Start);
        }
    }
}
