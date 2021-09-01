using System;
using System.Collections.Generic;
using System.Text;
using Markdig.Syntax;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
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
            SnapshotSpan extend = _view.Caret.ContainingTextViewLine.Extent;

            if (extend.IsEmpty)
                return false;

            //var text = _view.Caret.ContainingTextViewLine.Extent.GetText();

            if (!_view.TryParsePendingSmartBlock(true, out List<Block> blocks, out MarkdownDocument doc, out bool isEmptyLineText, out bool isEmptyLineTextAfterCaret))
                return false;

            string text = extend.GetText();
            int currentColumn = 0;
            for (; currentColumn < text.Length; currentColumn++)
            {
                if (!char.IsWhiteSpace(text[currentColumn])) // TODO: won't work well with tabs
                {
                    break;
                }
            }

            int position = extend.Start.Position;

            if (commandId == VSConstants.VSStd2KCmdID.TAB)
            {
                var builder = new StringBuilder();

                // This loop will try to find the next column stop based on the current syntax tree stack
                int nextColumnStop = 0;
                foreach (Block block in blocks)
                {
                    Block nextBlock = block;
                    while (nextBlock != null)
                    {
                        // If we are on a block that is the current line
                        if (nextBlock.Span.Start >= extend.Span.Start)
                        {
                            // If this is a list item, we should process the previews list item instead
                            if (nextBlock is ListItemBlock listItem)
                            {
                                var list = (ListBlock)listItem.Parent;
                                int index = list.IndexOf(listItem);
                                if (index > 0)
                                {
                                    nextBlock = list[index - 1];
                                }
                            }
                            else
                            {
                                // Otherwise don't need to continue
                                break;
                            }
                        }

                        // If the current block is at a higher column, we stop there
                        if (nextBlock.Column > currentColumn && nextBlock.Span.Start < position)
                        {
                            nextColumnStop = nextBlock.Column;
                            goto columnFound;
                        }

                        // Else we continue deep in the hierarchy to find a nested list item/elements
                        var container = nextBlock as ContainerBlock;
                        nextBlock = container?.LastChild;
                    }
                }

                columnFound:
                // If we haven't found a column stop, we will still indent by +2
                nextColumnStop = nextColumnStop - currentColumn <= 0 ? currentColumn + 2 : nextColumnStop;
                for (; currentColumn < nextColumnStop; currentColumn++)
                {
                    builder.Append(' ');
                }

                using (ITextEdit edit = _view.TextBuffer.CreateEdit())
                {
                    edit.Insert(position, builder.ToString());
                    edit.Apply();
                }
            }
            else if (commandId == VSConstants.VSStd2KCmdID.BACKTAB)
            {
                if (currentColumn == 0)
                    return false;

                // Try to find any previous tab stops
                int deleteCount = 0;
                for (int i = blocks.Count - 1; i >= 0; i--)
                {
                    Block block = blocks[i];
                    if (currentColumn > block.Column && block.Span.Start < position)
                    {
                        deleteCount = currentColumn - block.Column;
                        break;
                    }
                }

                // If we haven't found a tab stop, check that there is not a character that we could still remove
                if (currentColumn > 0 && deleteCount <= 0)
                {
                    deleteCount = currentColumn >= 2 ? 2 : currentColumn;
                }

                if (deleteCount > 0)
                {
                    using (ITextEdit edit = _view.TextBuffer.CreateEdit())
                    {

                        edit.Delete(position, deleteCount);
                        edit.Apply();
                    }
                }
            }

            return true;
        }


        protected override bool IsEnabled()
        {
            return MarkdownEditorPackage.Language.Preferences.IndentStyle == Microsoft.VisualStudio.Package.IndentingStyle.Smart;
        }
    }
}