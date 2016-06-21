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

        public static bool Match(string text)
        {
            MarkdownDocument doc;
            return Match(text, out doc);
        }

        public static bool Match(string text, out MarkdownDocument doc)
        {
            // Temp workaround for list items: We trim the beginning of the line to be able to parse nested list items.
            text = text.TrimStart();
            doc = Markdown.Parse(text, DefaultPipeline);
            return doc.Count != 0 && (doc[0] is QuoteBlock || doc[0] is ListBlock | doc[0] is CodeBlock || doc[0] is FooterBlock);
        }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var extend = _view.Caret.ContainingTextViewLine.Extent;
            var text = extend.GetText();

            if (!Match(text))
            {
                return false;
            }

            // Parse only until the end of the line after the caret
            // Because most of the time, a user would have typed characters before typing return
            // it is not efficient to re-use cached MarkdownDocument from Markdownfactory, as it may be invalid,
            // and then after the hit return, the browser would have to be updated with a new snapshot
            var snapshot = _view.TextBuffer.CurrentSnapshot;
            var textFromTop = snapshot.GetText(0, _view.Caret.ContainingTextViewLine.Extent.Span.End);
            var doc = Markdown.Parse(textFromTop, MarkdownFactory.Pipeline);
            var caretPosition = _view.Caret.Position.BufferPosition.Position;
            var lastChild = doc.FindBlockAtPosition(caretPosition);
            if (lastChild == null || !lastChild.ContainsPosition(caretPosition))
            {
                return false;
            }

            // Re-build list of blocks
            var blocks = new List<Block>();
            var block = lastChild;
            while (block != null)
            {
                // We don't add ListBlock (as they should have list items)
                if (block != doc && !(block is ListBlock))
                {
                    blocks.Add(block);
                }
                block = block.Parent;
            }
            blocks.Reverse();

            // If last child is null or with have just a task list, we have an empty line, so we can remove it
            if (!(lastChild is ParagraphBlock) || ((lastChild as ParagraphBlock)?.Inline?.LastChild is TaskList))
            {
                // We rebuild the new line up to the last parent container
                blocks.RemoveAt(blocks.Count - 1);
                var newLine = blocks.Count == 0 ? string.Empty : BuildNewLine(blocks);

                using (var edit = _view.TextBuffer.CreateEdit())
                {
                    edit.Delete(extend);
                    edit.Insert(extend.Start, string.IsNullOrEmpty(newLine) ? Environment.NewLine : newLine);
                    edit.Apply();
                }
            }
            else
            {
                var newLine = BuildNewLine(blocks);

                // Make 2 separate edits so the auto-insertion of list items can be undone (ctrl-z)
                _view.TextBuffer.Insert(caretPosition, Environment.NewLine);
                _view.TextBuffer.Insert(caretPosition + Environment.NewLine.Length, newLine);

            }
            return true;
        }

        private string BuildNewLine(List<Block> blocks)
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