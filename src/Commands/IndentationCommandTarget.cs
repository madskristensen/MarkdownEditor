using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class IndentationCommandTarget : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        public IndentationCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.TAB, VSConstants.VSStd2KCmdID.BACKTAB)
        { }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var extend = _view.Caret.ContainingTextViewLine.Extent;

            if (extend.IsEmpty)
                return false;

            var text = _view.Caret.ContainingTextViewLine.Extent.GetText();

            if (!SmartIndentCommandTarget.Match(text))
                return false;

            var position = extend.Start.Position;

            if (commandId == VSConstants.VSStd2KCmdID.TAB)
            {
                using (var edit = _view.TextBuffer.CreateEdit())
                {
                    edit.Insert(position, "  ");
                    edit.Apply();
                }
            }
            else if (commandId == VSConstants.VSStd2KCmdID.BACKTAB)
            {
                if (!CanDecrease(text))
                    return false;

                using (var edit = _view.TextBuffer.CreateEdit())
                {
                    edit.Delete(position, 2);
                    edit.Apply();
                }
            }

            return true;
        }

        private bool CanDecrease(string text)
        {
            if (text.Length <= 2)
                return false;

            return string.IsNullOrWhiteSpace(text.Substring(0, 2));
        }

        protected override bool IsEnabled()
        {
            return MarkdownEditorPackage.Options.EnableSmartIndent;
        }
    }
}