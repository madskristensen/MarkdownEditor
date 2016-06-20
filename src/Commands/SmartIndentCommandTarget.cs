using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class SmartIndentCommandTarget : CommandTargetBase<VSConstants.VSStd97CmdID>
    {
        public SmartIndentCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd97CmdID.LineBreak)
        { }

        protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            char typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            var text = _view.Caret.ContainingTextViewLine.Extent.GetText();

            var sb = new StringBuilder();

            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c))
                    sb.Append(c);
                else
                    break;
            }

            using (var edit = _view.TextBuffer.CreateEdit())
            {
                edit.Insert(_view.Caret.Position.BufferPosition, Environment.NewLine + sb.ToString());
                edit.Apply();
            }

            return true;
        }

        protected override bool IsEnabled()
        {
            return MarkdownEditorPackage.Options.EnableSmartIndent;
        }
    }
}