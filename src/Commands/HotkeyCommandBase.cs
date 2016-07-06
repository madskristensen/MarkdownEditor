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
    public abstract class HotkeyCommandBase : IOleCommandTarget
    {
        private IOleCommandTarget _nextCommandTarget;
        private readonly IWpfTextView _view;

        private readonly Guid _commandGroup;
        private readonly uint _commandId;

        public HotkeyCommandBase(IVsTextView adapter, IWpfTextView textView, Guid commandGroup, uint commandId)
        {
            _view = textView;
            _commandGroup = commandGroup;
            _commandId = commandId;

            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                ErrorHandler.ThrowOnFailure(adapter.AddCommandFilter(this, out _nextCommandTarget));
            }, DispatcherPriority.ApplicationIdle);
        }

        public abstract string Symbol { get; }

        public virtual string MultiLineSymbolStart
        {
            get { return Symbol; }
        }

        public virtual string MultiLineSymbolEnd
        {
            get { return Symbol; }
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!MarkdownEditorPackage.Options.EnableHotKeys)
                return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            if (pguidCmdGroup != _commandGroup || _commandId != nCmdID)
                return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            var span = _view.Selection.SelectedSpans.First().Span;
            string text = _view.TextBuffer.CurrentSnapshot.GetText(span);

            bool isMultiline = text.Contains('\n');
            string symbolStart = isMultiline ? MultiLineSymbolStart : Symbol;
            string symbolEnd = isMultiline ? MultiLineSymbolEnd : Symbol;

            using (var edit = _view.TextBuffer.CreateEdit())
            {
                edit.Replace(span, $"{symbolStart}{text}{symbolEnd}");
                edit.Apply();
            }

            var newSelectionSpan = new SnapshotSpan(_view.TextBuffer.CurrentSnapshot, span.Start, span.Length + symbolStart.Length + symbolEnd.Length);
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
