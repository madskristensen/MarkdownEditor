using System;
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
        /// The number of seconds to wait before updating the preview after an edit
        /// </summary>
        private const double RefreshAfterEditInSeconds = 0.5; // TODO: Make this configurable in the options?

        /// <summary>
        /// The number of seconds to wait before updating the preview after a change in position
        /// </summary>
        private const double RefreshAfterNewPositionInSeconds = 0.1; // TODO: Make this configurable in the options?


        public BrowserMargin(ITextView textview, ITextDocument document)
        {
            _textView = textview;

            _updaterDocument = new DispatcherTimer { Interval = TimeSpan.FromSeconds(RefreshAfterEditInSeconds) };
            _updaterDocument.Tick += UpdaterDocumentOnTick;

            _updaterPosition = new DispatcherTimer { Interval = TimeSpan.FromSeconds(RefreshAfterNewPositionInSeconds) };
            _updaterPosition.Tick += UpdaterPositionOnTick;

            _textView.LayoutChanged += LayoutChanged;

            _document = document;
            _document.TextBuffer.Changed += TextBufferChanged;

            _browser = new Browser(_document.FilePath);

            if (MarkdownEditorPackage.Options.ShowPreviewWindowBelow)
                CreateBottomMarginControls();
            else
                CreateRightMarginControls();

            UpdateBrowser();
        }

        public bool Enabled => true;
        public double MarginSize => MarkdownEditorPackage.Options.PreviewWindowWidth;
        public FrameworkElement VisualElement => this;

        private void LayoutChanged(object sender, TextViewLayoutChangedEventArgs textViewLayoutChangedEventArgs)
        {
            // Notifies a position update (but don't cancel the previous update if a new update is coming in between)
            //_updaterPosition.Stop();
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
                _browser.UpdatePosition(lineNumber);

            }), DispatcherPriority.ApplicationIdle, null);
        }

        private async void UpdateBrowser()
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                _browser.UpdateBrowser(_document.TextBuffer.CurrentSnapshot);

            }), DispatcherPriority.ApplicationIdle, null);
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return this;
        }

        private void CreateRightMarginControls()
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
            splitter.DragCompleted += RightDragCompleted;

            grid.Children.Add(splitter);
            Grid.SetColumn(splitter, 1);
            Grid.SetRow(splitter, 0);
        }

        private void CreateBottomMarginControls()
        {
            var height = MarkdownEditorPackage.Options.PreviewWindowHeight;

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(height, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            grid.Children.Add(_browser.Control);
            Children.Add(grid);

            Grid.SetColumn(_browser.Control, 0);
            Grid.SetRow(_browser.Control, 2);

            GridSplitter splitter = new GridSplitter();
            splitter.Height = 5;
            splitter.ResizeDirection = GridResizeDirection.Rows;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.DragCompleted += BottomDragCompleted;

            grid.Children.Add(splitter);
            Grid.SetColumn(splitter, 0);
            Grid.SetRow(splitter, 1);
        }

        void RightDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (!double.IsNaN(_browser.Control.ActualWidth))
            {
                MarkdownEditorPackage.Options.PreviewWindowWidth = _browser.Control.ActualWidth;
                MarkdownEditorPackage.Options.SaveSettingsToStorage();
            }
        }

        void BottomDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (!double.IsNaN(_browser.Control.ActualHeight))
            {
                MarkdownEditorPackage.Options.PreviewWindowHeight = _browser.Control.ActualHeight;
                MarkdownEditorPackage.Options.SaveSettingsToStorage();
            }
        }

        public void Dispose()
        {
            // Make sure timers are stopped
            _updaterDocument.Stop();
            _updaterPosition.Stop();

            // TODO: concurrency problem between stopping the DispatchTimer above and the following Browser dispose?

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
