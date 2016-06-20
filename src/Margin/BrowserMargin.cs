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

        public BrowserMargin(ITextDocument document)
        {
            _document = document;
            _document.FileActionOccurred += DocumentUpdated;

            _browser = new Browser(_document.FilePath);
            CreateMarginControls();
            UpdateBrowser();
        }

        public bool Enabled => true;
        public double MarginSize => MarkdownEditorPackage.Options.PreviewWindowWidth;
        public FrameworkElement VisualElement => this;

        private void DocumentUpdated(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                UpdateBrowser();
            }
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
                MarkdownEditorPackage.Options.SaveSettingsToStorage();
            }
        }

        public void Dispose()
        {
            if (_browser != null)
                _browser.Dispose();

            if (_document != null)
                _document.FileActionOccurred -= DocumentUpdated;
        }
    }
}
