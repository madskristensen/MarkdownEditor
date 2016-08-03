using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor
{
    class ConvertToTaskList : BaseSuggestedAction
    {
        private SnapshotSpan _span;
        private ITextView _view;

        public ConvertToTaskList(SnapshotSpan span, ITextView view)
        {
            _span = span;
            _view = view;
        }

        public override string DisplayText
        {
            get { return "Convert To Task List"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.CheckBoxList; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            SnapshotSpan span;
            var lines = GetSelectedLines(_span, out span);

            _view.Caret.MoveTo(lines.ElementAt(0).End);

            using (var edit = span.Snapshot.TextBuffer.CreateEdit())
            {
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line.GetText()))
                        edit.Insert(line.Start.Position, "- [ ] ");
                }

                edit.Apply();
            }

            _view.Selection.Clear();
        }
    }
}
