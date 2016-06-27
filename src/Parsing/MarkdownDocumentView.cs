using System;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Markdig.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor.Parsing
{
    public class MarkdownDocumentView
    {
        private readonly ITextView textView;
        private readonly DispatcherTimer updaterDocument;
        private readonly DispatcherTimer updaterPosition;
        private readonly DispatcherTimer updaterCaret;

        /// <summary>
        /// The number of seconds to wait before updating the preview after an edit
        /// </summary>
        private const double RefreshAfterEditInSeconds = 0.3; // TODO: Make this configurable in the options?

        /// <summary>
        /// The number of seconds to wait before updating the preview after a change in position
        /// </summary>
        private const double RefreshAfterNewPositionInSeconds = 0.1; // TODO: Make this configurable in the options?

        private MarkdownDocumentView(ITextView textView)
        {
            this.textView = textView;

            // Document updates
            updaterDocument = new DispatcherTimer { Interval = TimeSpan.FromSeconds(RefreshAfterEditInSeconds) };
            updaterDocument.Tick += UpdaterDocumentOnTick;
            textView.TextBuffer.Changed += OnDocumentChanged;

            // View position changed
            updaterPosition = new DispatcherTimer { Interval = TimeSpan.FromSeconds(RefreshAfterNewPositionInSeconds) };
            updaterPosition.Tick += UpdaterPositionOnTick;
            textView.LayoutChanged += OnPositionChanged;

            // Caret position changed
            updaterCaret = new DispatcherTimer { Interval = TimeSpan.FromSeconds(RefreshAfterEditInSeconds) };
            updaterCaret.Tick += UpdaterCaretOnTick;
            textView.Caret.PositionChanged += OnCaretPositionChanged;

            textView.Closed += TextViewOnClosed;
        }


        public event EventHandler DocumentChanged;

        public event EventHandler PositionChanged;

        public event EventHandler CaretChanged;

        public static MarkdownDocumentView Get(ITextView textView)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() => new MarkdownDocumentView(textView));
        }
        private void UpdaterDocumentOnTick(object sender, EventArgs eventArgs)
        {
            updaterDocument.Stop();
            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdaterPositionOnTick(object sender, EventArgs eventArgs)
        {
            updaterPosition.Stop();
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdaterCaretOnTick(object sender, EventArgs eventArgs)
        {
            updaterCaret.Stop();
            CaretChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnDocumentChanged(object sender, TextContentChangedEventArgs e)
        {
            updaterDocument.Stop();
            updaterDocument.Start();
        }

        private void OnPositionChanged(object sender, TextViewLayoutChangedEventArgs textViewLayoutChangedEventArgs)
        {
            // Notifies a position update (but don't cancel the previous update if a new update is coming in between)
            //_updaterPosition.Stop();
            updaterPosition.Start();
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            // For the caret, we will wait for a pause before trying to update
            updaterCaret.Stop();
            updaterCaret.Start();
        }

        private void TextViewOnClosed(object sender, EventArgs eventArgs)
        {
            // Make sure timers are stopped
            updaterDocument.Stop();
            updaterPosition.Stop();
            updaterCaret.Stop();

            textView.LayoutChanged -= OnPositionChanged;

            var textBuffer = textView.TextBuffer;
            if (textBuffer != null)
                textBuffer.Changed -= OnDocumentChanged;
        }
    }
}