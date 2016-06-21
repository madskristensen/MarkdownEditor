using System;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Footers;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
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
            doc = Markdown.Parse(text, DefaultPipeline);
            return doc.Count != 0 && (doc[0] is QuoteBlock || doc[0] is ListBlock | doc[0] is CodeBlock || doc[0] is FooterBlock);
        }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var extend = _view.Caret.ContainingTextViewLine.Extent;
            var text = extend.GetText();

            MarkdownDocument doc;

            if (!Match(text, out doc))
            {
                return false;
            }

            // Get the last container and last child
            ContainerBlock lastContainer = doc;
            Block firstChild = doc[0];
            var lastChild = firstChild;
            while (lastChild != null)
            {
                var container = lastChild as ContainerBlock;
                if (container != null)
                {
                    lastContainer = container;
                    lastChild = container.LastChild;
                }
                else
                {
                    break;
                }
            }

            // If last child is null or with have just a task list, we have an empty line, so we can remove it
            if (lastChild == null || ((lastChild as ParagraphBlock)?.Inline?.LastChild is TaskList))
            {
                // We rebuild the new line up to the last parent container
                var newLine = lastContainer == firstChild
                                  ? string.Empty
                                  : BuildNewLine(firstChild, lastContainer.Parent);

                using (var edit = _view.TextBuffer.CreateEdit())
                {
                    edit.Delete(extend);
                    edit.Insert(extend.Start, string.IsNullOrEmpty(newLine) ? Environment.NewLine : newLine);
                    edit.Apply();
                }
            }
            else
            {
                var newLine = BuildNewLine(firstChild, lastChild);
                var position = _view.Caret.Position.BufferPosition;

                // Make 2 separate edits so the auto-insertion of list items can be undone (ctrl-z)
                _view.TextBuffer.Insert(position, Environment.NewLine);
                _view.TextBuffer.Insert(position + Environment.NewLine.Length, newLine);

            }
            return true;
        }

        private string BuildNewLine(Block firstChild, Block lastChild)
        {
            var builder = new StringBuilder();

            var child = firstChild;
            var column = 0;

            // Special when there is a list but the last container is not a list
            // in this case, we will skip the list and keep other containers
            // when rebuilding the new line
            var lastContainer = lastChild as ContainerBlock ?? lastChild.Parent;
            bool skipList = !(lastContainer is ListItemBlock);

            while (child != null)
            {
                for (; column < child.Column; column++)
                {
                    builder.Append(' ');
                }

                if (!skipList && child is ListItemBlock)
                {
                    var listItem = (ListItemBlock) child;
                    var list = (ListBlock) listItem.Parent;

                    var startLength = builder.Length;
                    if (list.IsOrdered)
                    {
                        var c = list.OrderedStart[0];
                        if (c >= '0' && c <= '9')
                        {
                            int value;
                            int.TryParse(list.OrderedStart, out value);
                            value++;
                            builder.Append(value);
                        }
                        else if (c >= 'a' && c <= 'z')
                        {
                            c = (char)(c + 1);
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
                    var quoteBlock = (QuoteBlock)child;
                    builder.Append(quoteBlock.QuoteChar);
                    column++;
                }
                else if (child is ParagraphBlock)
                {
                    var paragraph = (ParagraphBlock)child;
                    if (paragraph.Inline?.FirstChild is TaskList)
                    {
                        builder.Append("[ ] ");
                        column += "[ ] ".Length;
                    }
                }
                else if (child is FooterBlock)
                {
                    var footerBlock = (FooterBlock)child;
                    builder.Append(footerBlock.OpeningCharacter);
                    builder.Append(footerBlock.OpeningCharacter);
                    column += 2;
                }

                if (child == lastChild)
                {
                    break;
                }

                var container = child as ContainerBlock;
                if (container != null)
                {
                    child = container.LastChild;
                }
                else
                {
                    break;
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