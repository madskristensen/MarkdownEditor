using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;

namespace MarkdownEditor
{
    public class LiveSyncMargin : DockPanel, IWpfTextViewMargin
    {
        private Image _image;
        private TextBlock _text;
        private BrowserMargin _browser;

        public LiveSyncMargin(IWpfTextView view)
        {
            SetResourceReference(BackgroundProperty, EnvironmentColors.ScrollBarBackgroundBrushKey);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            Loaded += (s, e) =>
            {
                if (!view.Properties.TryGetProperty(typeof(BrowserMargin), out _browser) && _browser.Browser != null)
                    return;

                CreateControls();
                UpdateControls();
            };
        }

        private void CreateControls()
        {
            var panel = new DockPanel();
            panel.HorizontalAlignment = HorizontalAlignment.Right;
            panel.VerticalAlignment = VerticalAlignment.Stretch;
            panel.Cursor = Cursors.Hand;
            panel.ToolTip = "Click to toggle scroll sync";
            panel.MouseUp += OnClick;
            Children.Add(panel);

            _text = new TextBlock();
            _text.Margin = new Thickness(0, 0, 5, 0);
            panel.Children.Add(_text);

            _image = new Image();
            _image.Margin = new Thickness(0, 1, 2, 0);
            panel.Children.Add(_image);
        }

        private void UpdateControls()
        {
            var moniker = _browser.Browser.AutoSyncEnabled ? KnownMonikers.Play : KnownMonikers.Pause;
            _image.Source = ImageHelper.GetImage(moniker, 11);
            _text.Text = "Scroll sync is " + (_browser.Browser.AutoSyncEnabled ? "active" : "paused");
        }

        private void OnClick(object sender, MouseButtonEventArgs e)
        {
            _browser.Browser.AutoSyncEnabled = !_browser.Browser.AutoSyncEnabled;
            UpdateControls();
        }

        public bool Enabled => true;

        public double MarginSize => 12;

        public FrameworkElement VisualElement
        {
            get { return this; }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return this;
        }
    }
}
