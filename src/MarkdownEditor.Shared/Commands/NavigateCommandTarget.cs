using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Syntax;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class Navigate : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        public Navigate(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.BOTTOMLINE, VSConstants.VSStd2KCmdID.TOPLINE)
        { }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var position = _view.Caret.Position.BufferPosition.Position;

            if (commandId == VSConstants.VSStd2KCmdID.TOPLINE)
                MoveCaretUp(position);
            else
                MoveCaretDown(position);

            return true;
        }

        private void MoveCaretDown(int position)
        {
            var headings = GetHeadings();
            var heading = headings.FirstOrDefault(h => h.Span.Start > position);

            if (heading != null)
                MoveCaretToBlock(heading);
        }

        private void MoveCaretUp(int position)
        {
            var headings = GetHeadings();
            var heading = headings.LastOrDefault(h => h.Span.Start < position);

            if (heading != null)
                MoveCaretToBlock(heading);
        }

        private void MoveCaretToBlock(MarkdownObject mdobj)
        {
            var point = new SnapshotPoint(_view.TextBuffer.CurrentSnapshot, mdobj.Span.Start);
            _view.Caret.MoveTo(point);

            var span = new SnapshotSpan(point, 0);
            _view.ViewScroller.EnsureSpanVisible(span);
        }

        private IEnumerable<HeadingBlock> GetHeadings()
        {
            var doc = _view.TextBuffer.CurrentSnapshot.ParseToMarkdown();
            return doc.Descendants().OfType<HeadingBlock>();
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}