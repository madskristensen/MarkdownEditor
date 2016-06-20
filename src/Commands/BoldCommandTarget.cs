using System;
using System.Linq;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class BoldCommandTarget : IOleCommandTarget
    {
        private IOleCommandTarget _nextCommandTarget;
        private readonly IWpfTextView _view;

        private static readonly Guid _commandGroup = new Guid("{c9dd4a59-47fb-11d2-83e7-00c04f9902c1}");
        private static readonly uint _commandId = 311; // maps to Debug.FunctionalBreakpoint
        private const string _symbol = "**";

        public BoldCommandTarget(IVsTextView adapter, IWpfTextView textView)
        {
            _view = textView;

            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                ErrorHandler.ThrowOnFailure(adapter.AddCommandFilter(this, out _nextCommandTarget));
            }, DispatcherPriority.ApplicationIdle);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!MarkdownEditorPackage.Options.EnableHotKeys)
                return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            if (pguidCmdGroup != _commandGroup || _commandId != nCmdID)
                return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            var span = _view.Selection.SelectedSpans.First().Span;
            string text = _view.TextBuffer.CurrentSnapshot.GetText(span);

            using (var edit = _view.TextBuffer.CreateEdit())
            {
                edit.Replace(span, $"{_symbol}{text}{_symbol}");
                edit.Apply();
            }

            var newSelectionSpan = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, span.Start, span.Length + _symbol.Length * 2);
            _view.Selection.Select(newSelectionSpan, _view.Selection.IsReversed);

            return VSConstants.S_OK;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (!MarkdownEditorPackage.Options.EnableHotKeys)
                return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

            if (pguidCmdGroup != _commandGroup)
                return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

            for (int i = 0; i < cCmds; i++)
            {
                if (_commandId == prgCmds[i].cmdID)
                {
                    if (!_view.Selection.IsEmpty)
                    {
                        prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                    }
                    else
                    {
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
                    }

                    return VSConstants.S_OK;
                }
            }

            return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}