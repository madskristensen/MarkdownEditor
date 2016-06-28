using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;
using Markdig.Renderers;
using Markdig.Syntax;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio.Text;
using mshtml;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using WebBrowser = System.Windows.Controls.WebBrowser;

namespace MarkdownEditor
{
    public class Browser : IDisposable
    {
        private string _file;
        private HTMLDocument _htmlDocument;
        private string _htmlTemplate;
        private int _zoomFactor;
        private double _cachedPosition = 0,
                       _cachedHeight = 0,
                       _positionPercentage = 0;

        private MarkdownDocument _currentDocument;
        private int _currentViewLine;

        [ThreadStatic]
        private static StringWriter htmlWriterStatic;

        public Browser(string file)
        {
            _zoomFactor = GetZoomFactor();
            _file = file;
            _htmlTemplate = GetHtmlTemplate();
            _currentViewLine = -1;

            InitBrowser();
        }

        public WebBrowser Control { get; private set; }

        public bool AutoSyncEnabled { get; set; } = MarkdownEditorPackage.Options.EnablePreviewSyncNavigation;

        private void InitBrowser()
        {
            Control = new WebBrowser();
            Control.HorizontalAlignment = HorizontalAlignment.Stretch;

            Control.LoadCompleted += (s, e) =>
            {
                Zoom(_zoomFactor);
                _htmlDocument = (HTMLDocument)Control.Document;

                _cachedHeight = _htmlDocument.body.offsetHeight;
                _htmlDocument.documentElement.setAttribute("scrollTop", _positionPercentage * _cachedHeight / 100);

                foreach (IHTMLElement link in _htmlDocument.links)
                {
                    HTMLAnchorElement anchor = link as HTMLAnchorElement;
                    if (anchor == null || anchor.protocol != "file:")
                        continue;

                    HTMLAnchorEvents_Event handler = anchor as HTMLAnchorEvents_Event;
                    if (handler == null)
                        continue;

                    string file = anchor.pathname.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    if (!File.Exists(file))
                    {
                        anchor.title = "The file does not exist";
                        return;
                    }

                    handler.onclick += () =>
                    {
                        ProjectHelpers.OpenFileInPreviewTab(file);
                        return true;
                    };
                }
            };

            // Open external links in default browser
            Control.Navigating += (s, e) =>
            {
                if (e.Uri == null)
                    return;

                e.Cancel = true;
                if (e.Uri.IsAbsoluteUri && e.Uri.Scheme.StartsWith("http"))
                    Process.Start(e.Uri.ToString());
            };
        }

        private static int GetZoomFactor()
        {
            using (Graphics g = Graphics.FromHwnd(Process.GetCurrentProcess().MainWindowHandle))
            {
                var baseLine = 96;
                var dpi = g.DpiX;

                if (baseLine == dpi)
                    return 100;

                // 150% scaling => 225
                // 250% scaling => 400

                double scale = dpi * ((dpi - baseLine) / baseLine + 1);
                return Convert.ToInt32(Math.Ceiling(scale / 25)) * 25; // round up to nearest 25
            }
        }

        public void UpdatePosition(int line)
        {
            if (_htmlDocument != null && _currentDocument != null && AutoSyncEnabled)
            {
                _currentViewLine = _currentDocument.FindClosestLine(line);
                SyncNavigation();
            }
        }

        private void SyncNavigation()
        {
            if (_htmlDocument == null)
            {
                return;
            }

            if (AutoSyncEnabled)
            {
                if (_currentViewLine == 0)
                {
                    // Forces the preview window to scroll to the top of the document
                    _htmlDocument.documentElement.setAttribute("scrollTop", 0);
                }
                else
                {
                    var element = _htmlDocument.getElementById("pragma-line-" + _currentViewLine);
                    if (element != null)
                    {
                        element.scrollIntoView(true);
                    }
                }
            }
            else if (_htmlDocument != null)
            {
                _currentViewLine = -1;
                _cachedPosition = _htmlDocument.documentElement.getAttribute("scrollTop");
                _cachedHeight = Math.Max(1.0, _htmlDocument.body.offsetHeight);
                _positionPercentage = _cachedPosition * 100 / _cachedHeight;
            }
        }

        public async Task UpdateBrowser(ITextSnapshot snapshot)
        {
            await Control.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Generate the HTML document
                string html = null;
                StringWriter htmlWriter = null;
                try
                {
                    _currentDocument = snapshot.ParseToMarkdown();

                    htmlWriter = htmlWriterStatic ?? (htmlWriterStatic = new StringWriter());
                    htmlWriter.GetStringBuilder().Clear();
                    var htmlRenderer = new HtmlRenderer(htmlWriter);
                    MarkdownFactory.Pipeline.Setup(htmlRenderer);
                    htmlRenderer.Render(_currentDocument);
                    htmlWriter.Flush();
                    html = htmlWriter.ToString();
                }
                catch (Exception ex)
                {
                    // We could output this to the exception pane of VS?
                    // Though, it's easier to output it directly to the browser
                    html = "<p>An unexpected exception occured:</p><pre>" +
                           ex.ToString().Replace("<", "&lt;").Replace("&", "&amp;") + "</pre>";
                }
                finally
                {
                    // Free any resources allocated by HtmlWriter
                    htmlWriter?.GetStringBuilder().Clear();
                }

                if (_htmlDocument != null)
                {
                    var content = _htmlDocument.getElementById("___markdown-content___");
                    content.innerHTML = html;

                    // Makes sure that any code blocks get syntax highligted by Prism
                    var win = _htmlDocument.parentWindow;
                    win.execScript("Prism.highlightAll();", "javascript");
                }
                else
                {
                    var template = string.Format(CultureInfo.InvariantCulture, _htmlTemplate, html);
                    Logger.LogOnError(() => Control.NavigateToString(template));
                }

                SyncNavigation();
            }), DispatcherPriority.ApplicationIdle, null);
        }

        private static string GetFolder()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(assembly);
            return folder;
        }

        private string GetHtmlTemplate()
        {
            var baseHref = Path.GetDirectoryName(_file).Replace("\\", "/");
            string folder = GetFolder();
            string cssPath = Path.Combine(folder, "margin\\highlight.css");
            string scriptPath = Path.Combine(folder, "margin\\prism.js");

            return $@"<!DOCTYPE html>
<html lang=""en"">
    <head>
        <meta http-equiv=""X-UA-Compatible"" content=""IE=Edge"" />
        <meta charset=""utf-8"" />
        <base href=""file:///{baseHref}/"" />
        <title>Markdown Preview</title>
        <link rel=""stylesheet"" href=""{cssPath}"" />
</head>
    <body class=""markdown-body"">
        <div id='___markdown-content___'>
          {{0}}
        </div>
        <script src=""{scriptPath}""></script>
    </body>
</html>";
        }

        private void Zoom(int zoomFactor)
        {
            if (zoomFactor == 100)
                return;

            dynamic OLECMDEXECOPT_DODEFAULT = 0;
            dynamic OLECMDID_OPTICAL_ZOOM = 63;
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);

            if (fiComWebBrowser == null)
                return;

            object objComWebBrowser = fiComWebBrowser.GetValue(Control);

            if (objComWebBrowser == null)
                return;

            objComWebBrowser.GetType().InvokeMember("ExecWB", BindingFlags.InvokeMethod, null, objComWebBrowser, new object[] {
                OLECMDID_OPTICAL_ZOOM,
                OLECMDEXECOPT_DODEFAULT,
                zoomFactor,
                IntPtr.Zero
            });
        }

        public void Dispose()
        {
            if (Control != null)
                Control.Dispose();

            _htmlDocument = null;
        }
    }
}
