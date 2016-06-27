using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Markdig.Renderers;
using Markdig.Syntax;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor
{
    public sealed class HeadingNagivationMargin : DockPanel, IWpfTextViewMargin
    {
        public const string MarginName = "MarkdownMargin";
        private readonly ITextView textView;
        private readonly ComboBox headingCombo;

        private MarkdownDocument currentDocument;
        private int pendingChanges;
        private List<HeadingBlock> headings;

        public HeadingNagivationMargin(ITextView textView)
        {
            this.textView = textView;
            headingCombo = new ComboBox
            {
                FontFamily = new FontFamily("Consolas")
            };
            headingCombo.SelectionChanged += HandleHeadingComboSelectionChanged;

            var documentView = MarkdownDocumentView.Get(textView);
            documentView.DocumentChanged += OnDocumentChanged;
            documentView.CaretChanged += OnCaretPositionChanged;

            // Not working, which EnvironmentColors to use to get a white background for light theme in VS?
            //headingCombo.SetResourceReference(BackgroundProperty, EnvironmentColors.EditorExpansionFillBrushKey);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            this.Children.Add(headingCombo);

            RefreshCombo(this.textView.TextSnapshot);
        }

        public FrameworkElement VisualElement => this;

        public bool Enabled { get; } = true;

        public double MarginSize => this.Height;

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return this;
        }

        public void Dispose()
        {
        }

        private void OnCaretPositionChanged(object sender, EventArgs e)
        {
            if (headings == null)
            {
                return;
            }
            UpdateFromCaret();
        }

        private void OnDocumentChanged(object sender, EventArgs textContentChangedEventArgs)
        {
            RefreshComboItemsAsync(textView.TextSnapshot);
        }

        private void HandleHeadingComboSelectionChanged(object sender, EventArgs e)
        {
            if (pendingChanges > 0)
                return;

            var wrap = headingCombo.SelectedItem as HeadingWrap;
            MoveCaretToPosition(wrap?.Heading.Span.Start ?? 0);
        }

        private void MoveCaretToPosition(int position)
        {
            textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(textView.TextSnapshot, position, 1), EnsureSpanVisibleOptions.ShowStart);
            textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, position));
        }

        private async void RefreshComboItemsAsync(ITextSnapshot snapshot)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshCombo(snapshot);
            }), DispatcherPriority.ApplicationIdle, null);
        }

        private async void UpdateFromCaret()
        {
            var firstLinePosition = textView.Caret.Position.BufferPosition.Position;
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                Interlocked.Increment(ref pendingChanges);
                try
                {
                    UpdateComboFromPosition(firstLinePosition);
                }
                finally
                {
                    Interlocked.Decrement(ref pendingChanges);
                }
            }), DispatcherPriority.ApplicationIdle, null);
        }

        private void RefreshCombo(ITextSnapshot snapshot)
        {
            Interlocked.Increment(ref pendingChanges);
            try
            {
                var doc = snapshot.ParseToMarkdown();
                if (doc == currentDocument)
                {
                    return;
                }

                currentDocument = doc;
                headings = doc.OfType<HeadingBlock>().ToList();

                headingCombo.Items.Clear();
                headingCombo.Items.Add("(top)");

                var levels = new int[10];
                foreach (var heading in headings)
                {
                    if (heading.Level < levels.Length)
                    {
                        levels[heading.Level]++;
                    }
                    for (int j = heading.Level + 1; j < levels.Length; j++)
                    {
                        levels[j] = 0;
                    }
                    headingCombo.Items.Add(new HeadingWrap(heading, levels));
                }

                var position = textView.Selection.Start.Position;
                UpdateComboFromPosition(position);
            }
            finally
            {
                Interlocked.Decrement(ref pendingChanges);
            }
        }

        private void UpdateComboFromPosition(int position)
        {
            var currentIndex = -1;
            var localHeadings = headings; // work on a copy of the variable
            for (int i = localHeadings.Count - 1; i >= 0; i--)
            {
                var span = localHeadings[i].Span;
                if (position >= span.Start)
                {
                    currentIndex = i;
                    break;
                }
            }
            headingCombo.SelectedIndex = currentIndex + 1;
        }

        private class HeadingWrap
        {
            private readonly string headingText;

            public HeadingWrap(HeadingBlock heading, int[] levels)
            {
                this.Heading = heading;

                // We start to indent at the very effective starting level (Someone may start the doc with a ### instead of #)
                int startIndent = 0;
                for (; startIndent <= heading.Level; startIndent++)
                {
                    if (levels[startIndent] != 0)
                    {
                        break;
                    }
                }

                var builder = new StringBuilder();
                for (int i = 1; i < heading.Level; i++)
                {
                    if (i >= startIndent)
                    {
                        builder.Append(' ');
                    }
                }

                var stringWriter = new StringWriter();
                var htmlRenderer = new HtmlRenderer(stringWriter) { EnableHtmlForInline = false };
                htmlRenderer.Render(heading.Inline);
                stringWriter.Flush();
                headingText = stringWriter.ToString();

                // If the heading doesn't start by a digit, precalculate one
                if (headingText.Length == 0 || !char.IsDigit(headingText[0]))
                {
                    // If there are any intermediate levels not used, don't print them
                    // so # followed by ### is equivalent to # followed by ##
                    bool hasDigit = false;
                    bool hasDot = false;
                    for (int i = 1; i <= heading.Level; i++)
                    {
                        if (hasDigit && !hasDot)
                        {
                            builder.Append('.');
                            hasDot = true;
                        }

                        if (i < levels.Length && levels[i] != 0)
                        {
                            builder.Append(levels[i]);
                            hasDot = false;
                            hasDigit = true;
                        }
                    }

                    builder.Append(' ');
                }

                builder.Append(headingText);

                headingText = builder.ToString();
            }

            public HeadingBlock Heading { get; }

            public override string ToString()
            {
                return headingText;
            }
        }
    }
}
