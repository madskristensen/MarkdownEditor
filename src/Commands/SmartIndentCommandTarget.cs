using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Footers;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class SmartIndentCommandTarget : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        private static readonly MarkdownPipeline DefaultPipeline;

        static SmartIndentCommandTarget()
        {
            // Use a bare bone pipeline
            DefaultPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        }

        public SmartIndentCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.RETURN)
        {
        }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var extend = _view.Caret.ContainingTextViewLine.Extent;

            List<Block> blocks;
            MarkdownDocument doc;
            bool isEmptyLineText;
            bool isEmptyLineTextAfterCaret;
            // Try to parse the current line and current context (from the beginning of the document) to ensure proper smart indentation
            if (!_view.TryParsePendingSmartBlock(false, out blocks, out doc, out isEmptyLineText, out isEmptyLineTextAfterCaret))
            {
                return false;
            }

            var caretPosition = _view.Caret.Position.BufferPosition.Position;
            // If the line doesn't contain any text and enter is pressed, we have an "empty text line"
            if (isEmptyLineText)
            {
                // We preprend the line with the current stack of blocks minus 1 
                var linePosition = _view.TextBuffer.CurrentSnapshot.GetLineFromPosition(caretPosition).LineNumber;

                var block = blocks[blocks.Count - 1];
                // special case when the last block is a ListItem that is starting on a previous line (but current line is empty, due to markdown lazy continuation)
                bool isEmptyListItemBlockFromPreviousLine = block is ListItemBlock && block.Line < linePosition;
                blocks.RemoveAt(blocks.Count - 1);

                // If we have a list item not part of the current line, we can remove it
                if (isEmptyListItemBlockFromPreviousLine && blocks.Count > 0)
                {
                    blocks.RemoveAt(blocks.Count - 1);
                }

                // If we don't have any pending block in the stack, we can replace the whole line by a newline
                // otherwise we need to replace it with the current stack
                var newLine = blocks.Count == 0 ? Environment.NewLine : BuildNewLineFromBlockStack(blocks);

                using (var edit = _view.TextBuffer.CreateEdit())
                {
                    edit.Delete(extend);
                    edit.Insert(extend.Start, newLine);
                    edit.Apply();
                }
            }
            else
            {
                // Make 3 separate edits so the auto-insertion of list items can be undone (ctrl-z)
                var newLine = BuildNewLineFromBlockStack(blocks);
                
                // 1) The new line
                _view.TextBuffer.Insert(caretPosition, Environment.NewLine);

                // 2) An indent on the new line with only spaces
                _view.TextBuffer.Insert(caretPosition + Environment.NewLine.Length, new string(' ', newLine.Length));

                // 3) delete of the previous indent, add an indent with the current pending block structure (list items, blockquotes)
                using (var edit = _view.TextBuffer.CreateEdit())
                {
                    edit.Delete(new Span(caretPosition + Environment.NewLine.Length, newLine.Length));
                    edit.Insert(caretPosition + Environment.NewLine.Length, newLine);
                    edit.Apply();
                }
            }
            return true;
        }

        /// <summary>
        /// Builds a text representation of the pending blocks in the stack (with blockquotes, pending lists...etc)
        /// </summary>
        /// <param name="blocks">The blocks.</param>
        /// <returns>System.String.</returns>
        private string BuildNewLineFromBlockStack(List<Block> blocks)
        {
            var builder = new StringBuilder();

            var column = 0;

            // Special when there is a list but the last container is not a list
            // in this case, we will skip the list and keep other containers
            // when rebuilding the new line
            var lastChild = blocks[blocks.Count - 1];
            var lastListItemIndex = blocks.FindLastIndex(block => block is ListItemBlock);
            var emitLastListItem = lastListItemIndex == blocks.Count - 1 || (lastChild is ParagraphBlock && lastListItemIndex == blocks.Count - 2);

            for (int i = 0; i < blocks.Count; i++)
            {
                var child = blocks[i];
                for (; column < child.Column; column++)
                {
                    builder.Append(' ');
                }

                if (emitLastListItem && i == lastListItemIndex)
                {
                    var listItem = (ListItemBlock) child;
                    var list = (ListBlock) listItem.Parent;

                    var startLength = builder.Length;
                    if (list.IsOrdered)
                    {
                        var offset = list.IndexOf(listItem);
                        var c = list.OrderedStart[0];
                        if (c >= '0' && c <= '9')
                        {
                            int value;
                            int.TryParse(list.OrderedStart, out value);
                            value += offset;
                            value++;
                            builder.Append(value);
                        }
                        else if (c >= 'a' && c <= 'z')
                        {
                            c = (char) (c + offset + 1);
                            c = c > 'z' ? 'z' : c;
                            builder.Append(c);
                        }
                        else
                        {
                            // We don't know how to generate a new item so we replicate it (as it is valid in Markdown)
                            builder.Append(list.OrderedStart);
                        }

                        builder.Append(list.OrderedDelimiter);
                    }
                    else
                    {
                        builder.Append(list.BulletType);
                    }

                    // A list requires at least one space after the bullet
                    builder.Append(' ');

                    // Shift column state
                    column += builder.Length - startLength;
                }
                else if (child is QuoteBlock)
                {
                    var quoteBlock = (QuoteBlock) child;
                    builder.Append(quoteBlock.QuoteChar);
                    column++;
                }
                else if (child is ParagraphBlock)
                {
                    var paragraph = (ParagraphBlock) child;
                    if (paragraph.Inline?.FirstChild is TaskList)
                    {
                        builder.Append("[ ] ");
                        column += "[ ] ".Length;
                    }
                }
                else if (child is FooterBlock)
                {
                    var footerBlock = (FooterBlock) child;
                    builder.Append(footerBlock.OpeningCharacter);
                    builder.Append(footerBlock.OpeningCharacter);
                    column += 2;
                }
            }
            return builder.ToString();
        }

        protected override bool IsEnabled()
        {
            return MarkdownEditorPackage.Options.EnableSmartIndent;
        }
    }
}