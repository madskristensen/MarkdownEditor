using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor
{
    public class BrowserMargin : DockPanel, IWpfTextViewMargin
    {
        private readonly Browser _browser;
        private readonly ITextDocument _document;
        private readonly DispatcherTimer _updaterDocument;
        private readonly DispatcherTimer _updaterPosition;
        private readonly ITextView _textView;

        /// <summary>
        /// The number of seconds to wait before updating the position/document
        /// TODO: We may want this to be configurable in the settings
        /// </summary>
        private const double RefreshAfterSeconds = 0.5;

        public BrowserMargin(ITextView textview, ITextDocument document)
        {
            _textView = textview;

            _updaterDocument = new DispatcherTimer {Interval = TimeSpan.FromSeconds(RefreshAfterSeconds) };
            _updaterDocument.Tick += UpdaterDocumentOnTick;

            _updaterPosition = new DispatcherTimer { Interval = TimeSpan.FromSeconds(RefreshAfterSeconds) };
            _updaterPosition.Tick += UpdaterPositionOnTick;

            _textView.LayoutChanged += LayoutChanged;

            _document = document;
            _document.TextBuffer.Changed += TextBufferChanged;

            _browser = new Browser(_document.FilePath);
            CreateMarginControls();
            UpdateBrowser();
        }

        public bool Enabled => true;
        public double MarginSize => MarkdownEditorPackage.Options.PreviewWindowWidth;
        public FrameworkElement VisualElement => this;

        private void LayoutChanged(object sender, TextViewLayoutChangedEventArgs textViewLayoutChangedEventArgs)
        {
            _updaterPosition.Stop();
            _updaterPosition.Start();
        }

        private void UpdaterDocumentOnTick(object sender, EventArgs eventArgs)
        {
            _updaterDocument.Stop();
            UpdateBrowser();
        }

        private void UpdaterPositionOnTick(object sender, EventArgs eventArgs)
        {
            _updaterPosition.Stop();
            UpdatePosition();
        }

        private void TextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            _updaterDocument.Stop();
            _updaterDocument.Start();
        }

        private async void UpdatePosition()
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                var lineNumber = _textView.TextSnapshot.GetLineNumberFromPosition(_textView.TextViewLines.FirstVisibleLine.Start.Position);

                Trace.WriteLine($"Linenumber: {lineNumber}");

                _browser.UpdatePosition(lineNumber);

            }), DispatcherPriority.ApplicationIdle, null);
        }

        private async void UpdateBrowser()
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                var markdown = _document.TextBuffer.CurrentSnapshot.GetText();
                _browser.UpdateBrowser(markdown);

            }), DispatcherPriority.ApplicationIdle, null);
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return this;
        }

        protected virtual void CreateMarginControls()
        {
            var width = MarkdownEditorPackage.Options.PreviewWindowWidth;

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(width, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition());

            grid.Children.Add(_browser.Control);
            Children.Add(grid);

            Grid.SetColumn(_browser.Control, 2);
            Grid.SetRow(_browser.Control, 0);

            GridSplitter splitter = new GridSplitter();
            splitter.Width = 5;
            splitter.ResizeDirection = GridResizeDirection.Columns;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.DragCompleted += splitter_DragCompleted;

            grid.Children.Add(splitter);
            Grid.SetColumn(splitter, 1);
            Grid.SetRow(splitter, 0);
        }

        void splitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (!double.IsNaN(_browser.Control.ActualWidth))
            {
                MarkdownEditorPackage.Options.PreviewWindowWidth = _browser.Control.ActualWidth;
            }
        }

        public void Dispose()
        {
            if (_browser != null)
                _browser.Dispose();

            if (_textView != null)
                _textView.LayoutChanged -= LayoutChanged;

            var textBuffer = _document?.TextBuffer;
            if (textBuffer != null)
                textBuffer.Changed -= TextBufferChanged;
        }
    }
}
