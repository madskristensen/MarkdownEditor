using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor
{
    public class BrowserMargin : DockPanel, IWpfTextViewMargin
    {
        private readonly ITextDocument _document;
        private readonly ITextView _textView;

        public BrowserMargin(ITextView textview, ITextDocument document)
        {
            _textView = textview;
            _document = document;

            Browser = new Browser(_document.FilePath);

            if (MarkdownEditorPackage.Options.ShowPreviewWindowBelow)
                CreateBottomMarginControls();
            else
                CreateRightMarginControls();

            UpdateBrowser();

            var documentView = MarkdownDocumentView.Get(textview);
            documentView.DocumentChanged += UpdaterDocumentOnTick;
            documentView.PositionChanged += UpdaterPositionOnTick;
        }

        public bool Enabled => true;
        public double MarginSize => 500;
        public FrameworkElement VisualElement => this;
        public Browser Browser { get; private set; }

        private void UpdaterDocumentOnTick(object sender, EventArgs eventArgs)
        {
            UpdateBrowser();
        }

        private async void UpdaterPositionOnTick(object sender, EventArgs eventArgs)
        {
            await UpdatePosition();
        }

        private async Task UpdatePosition()
        {
            var lineNumber = _textView.TextSnapshot.GetLineNumberFromPosition(_textView.TextViewLines.FirstVisibleLine.Start.Position);
            Trace.WriteLine($"UpdatePosition {lineNumber}");
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                Browser.UpdatePosition(lineNumber);

            }), DispatcherPriority.ApplicationIdle, null);
        }

        private async void UpdateBrowser()
        {
            await Browser.UpdateBrowser(_document.TextBuffer.CurrentSnapshot);
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
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(width, GridUnitType.Pixel), MinWidth = 150 });
            grid.RowDefinitions.Add(new RowDefinition());
            Children.Add(grid);

            grid.Children.Add(Browser.Control);
            Grid.SetColumn(Browser.Control, 2);
            Grid.SetRow(Browser.Control, 0);

            GridSplitter splitter = new GridSplitter();
            splitter.Width = 5;
            splitter.ResizeDirection = GridResizeDirection.Columns;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.DragCompleted += RightDragCompleted;

            grid.Children.Add(splitter);
            Grid.SetColumn(splitter, 1);
            Grid.SetRow(splitter, 0);

            var fixWidth = new Action(() =>
            {
                // previewWindow maxWidth = current total width - textView minWidth
                var newWidth = (_textView.ViewportWidth + grid.ActualWidth) - 150;

                // preveiwWindow maxWidth < previewWindow minWidth
                if (newWidth < 150)
                {
                    // Call 'get before 'set for performance
                    if (grid.ColumnDefinitions[2].MinWidth != 0)
                    {
                        grid.ColumnDefinitions[2].MinWidth = 0;
                        grid.ColumnDefinitions[2].MaxWidth = 0;
                    }
                }
                else
                {
                    grid.ColumnDefinitions[2].MaxWidth = newWidth;
                    // Call 'get before 'set for performance
                    if (grid.ColumnDefinitions[2].MinWidth == 0)
                        grid.ColumnDefinitions[2].MinWidth = 150;
                }
            });

            // Listen sizeChanged event of both marginGrid and textView
            grid.SizeChanged += (e, s) => fixWidth();
            _textView.ViewportWidthChanged += (e, s) => fixWidth();
        }

        private void CreateBottomMarginControls()
        {
            var height = MarkdownEditorPackage.Options.PreviewWindowHeight;

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(height, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            grid.Children.Add(Browser.Control);
            Children.Add(grid);

            Grid.SetColumn(Browser.Control, 0);
            Grid.SetRow(Browser.Control, 2);

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
            if (!double.IsNaN(Browser.Control.ActualWidth))
            {
                MarkdownEditorPackage.Options.PreviewWindowWidth = Browser.Control.ActualWidth;
                MarkdownEditorPackage.Options.SaveSettingsToStorage();
            }
        }

        void BottomDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (!double.IsNaN(Browser.Control.ActualHeight))
            {
                MarkdownEditorPackage.Options.PreviewWindowHeight = Browser.Control.ActualHeight;
                MarkdownEditorPackage.Options.SaveSettingsToStorage();
            }
        }

        public void Dispose()
        {
            // TODO: concurrency problem between stopping the DispatchTimer above and the following Browser dispose?

            if (Browser != null)
                Browser.Dispose();

        }
    }
}
