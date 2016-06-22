using System;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class ToogleTaskCommandTarget : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        private SnapshotSpan _span;

        public ToogleTaskCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.COMPLETEWORD)
        { }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_span == null || !_view.Selection.IsEmpty)
                return false;

            var text = _span.GetText();

            var indexOfUnchecked = text.IndexOf("[ ]");
            if (indexOfUnchecked > -1)
            {
                text = text
                    .Remove(indexOfUnchecked, 3)
                    .Insert(indexOfUnchecked, "[x]");
            }
            else
            {
                var indexOfChecked = text.IndexOf("[x]");
                if (indexOfChecked > -1)
                {
                    text = text
                        .Remove(indexOfChecked, 3)
                        .Insert(indexOfChecked, "[ ]");
                }
            }

            using (var edit = _view.TextBuffer.CreateEdit())
            {
                edit.Replace(_span, text);
                edit.Apply();
            }

            return true;
        }

        protected override bool IsEnabled()
        {
            var start = _view.Caret.ContainingTextViewLine.Start;
            var length = _view.Caret.ContainingTextViewLine.Length;

            _span = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, start, length);

            var text = _span.GetText();

            return MarkdownFactory.MatchSmartBlock(text);
        }
    }
}