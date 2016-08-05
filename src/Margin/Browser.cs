using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
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
            _currentViewLine = -1;

            InitBrowser();

            CssCreationListener.StylesheetUpdated += OnStylesheetUpdated;
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

                this.AdjustAnchors();
            };

            // Open external links in default browser
            Control.Navigating += (s, e) =>
            {
                if (e.Uri == null)
                    return;

                e.Cancel = true;

                // If it's a file-based anchor we converted, open the related file if possible
                if (e.Uri.Scheme == "about")
                {
                    string file = e.Uri.LocalPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

                    // In-page section link.  Not something we can handle here so ignore it.  Note that they
                    // don't work anyway even if this event isn't canceled.
                    if (file == "blank")
                        return;

                    if (!File.Exists(file))
                    {
                        string[] possibleExtensions = new string[] { ".markdown", ".md", ".mdown", ".mdwn", ".mkd", ".mkdn", ".mmd", ".rst" };
                        string ext = null;

                        // If the file has no extension, see if one exists with a markdown extension.  If so,
                        // treat it as the file to open.
                        if (String.IsNullOrEmpty(Path.GetExtension(file)))
                            ext = possibleExtensions.FirstOrDefault(fx => File.Exists(file + fx));

                        if (ext != null)
                            ProjectHelpers.OpenFileInPreviewTab(file + ext);
                    }
                    else
                        ProjectHelpers.OpenFileInPreviewTab(file);
                }
                else
                    if (e.Uri.IsAbsoluteUri && e.Uri.Scheme.StartsWith("http"))
                        Process.Start(e.Uri.ToString());
            };
        }

        /// <summary>
        /// Adjust the file-based anchors so that they are navigable on the local file system
        /// </summary>
        /// <remarks>Anchors using the "file:" protocol appear to be blocked by security settings and won't work.
        /// If we convert them to use the "about:" protocol so that we recognize them, we can open the file in
        /// the <c>Navigating</c> event handler.</remarks>
        private void AdjustAnchors()
        {
            try
            {
                foreach (IHTMLElement link in _htmlDocument.links)
                {
                    HTMLAnchorElement anchor = link as HTMLAnchorElement;

                    if (anchor != null && anchor.protocol == "file:")
                    {
                        string pathName = null, hash = anchor.hash;

                        // Anchors with a hash cause a crash if you try to set the protocol without clearing the
                        // hash and path name first.
                        if (hash != null)
                        {
                            pathName = anchor.pathname;
                            anchor.hash = null;
                            anchor.pathname = String.Empty;
                        }

                        anchor.protocol = "about:";

                        if (hash != null)
                        {
                            // For an in-page section link, use "blank" as the path name.  These don't work
                            // anyway but this is the proper way to handle them.
                            if (pathName == null || pathName.EndsWith("/"))
                                pathName = "blank";

                            anchor.pathname = pathName;
                            anchor.hash = hash;
                        }
                    }
                }
            }
            catch
            {
                // Ignore exceptions
            }
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
                    html = "<p>An unexpected exception occurred:</p><pre>" +
                           ex.ToString().Replace("<", "&lt;").Replace("&", "&amp;") + "</pre>";
                }
                finally
                {
                    // Free any resources allocated by HtmlWriter
                    htmlWriter?.GetStringBuilder().Clear();
                }

                IHTMLElement content = null;

                if (_htmlDocument != null)
                    content = _htmlDocument.getElementById("___markdown-content___");

                // Content may be null if the Refresh context menu option is used.  If so, reload the template.
                if (content != null)
                {
                    content.innerHTML = html;

                    // Makes sure that any code blocks get syntax highlighted by Prism
                    var win = _htmlDocument.parentWindow;
                    win.execScript("Prism.highlightAll();", "javascript");

                    // Adjust the anchors after and edit
                    this.AdjustAnchors();
                }
                else
                {
                    var htmlTemplate = GetHtmlTemplate();
                    var template = string.Format(CultureInfo.InvariantCulture, htmlTemplate, html);
                    Logger.LogOnError(() => Control.NavigateToString(template));
                }

                SyncNavigation();
            }), DispatcherPriority.ApplicationIdle, null);
        }

        private void OnStylesheetUpdated(object sender, EventArgs e)
        {
            if (_htmlDocument != null)
            {
                var link = _htmlDocument.styleSheets.item(0) as IHTMLStyleSheet;

                if (link != null)
                {
                    link.href = GetCustomStylesheet(_file) + "?" + new Guid();
                }
            }
        }

        public static string GetFolder()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assembly);
        }

        private string GetHtmlTemplate()
        {
            var baseHref = Path.GetDirectoryName(_file).Replace("\\", "/");
            string folder = GetFolder();
            string cssPath = GetCustomStylesheet(_file) ?? Path.Combine(folder, "margin\\highlight.css");
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

        private static string GetCustomStylesheet(string markdownFile)
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(markdownFile));
            var name = MarkdownEditorPackage.Options.CustomStylesheetFileName;

            while (dir.Parent != null)
            {
                string file = Path.Combine(dir.FullName, name);

                if (File.Exists(file))
                    return file;

                dir = dir.Parent;
            }

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string globalFile = Path.Combine(userProfile, name);

            if (File.Exists(globalFile))
                return globalFile;

            return null;
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
