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
            Cursor = Cursors.Hand;

            Loaded += (s, e) =>
            {
                if (!view.Properties.TryGetProperty(typeof(BrowserMargin), out _browser) && _browser.Browser != null)
                    return;

                var panel = new DockPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    ToolTip = "Click to toggle live sync",
                };

                panel.MouseUp += OnClick;

                Children.Add(panel);

                _image = new Image
                {
                    Source = ImageHelper.GetImage(KnownMonikers.Refresh, 12),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 5, 0),
                };

                _text = new TextBlock
                {
                    Padding = new Thickness(5),
                    HorizontalAlignment = HorizontalAlignment.Right,
                };

                panel.Children.Add(_text);
                panel.Children.Add(_image);

                UpdateControls();
            };
        }

        private void UpdateControls()
        {
            var moniker = _browser.Browser.AutoSyncEnabled ? KnownMonikers.StatusRunning : KnownMonikers.StatusPaused;
            _image.Source = ImageHelper.GetImage(moniker, 12);
            _text.Text = "Scroll sync is " + (_browser.Browser.AutoSyncEnabled ? "enabled" : "disabled");
        }

        private void OnClick(object sender, MouseButtonEventArgs e)
        {
            _browser.Browser.AutoSyncEnabled = !_browser.Browser.AutoSyncEnabled;
            UpdateControls();
        }

        public bool Enabled => true;

        public double MarginSize => 16;

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
