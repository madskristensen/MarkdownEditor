using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Markdig.Renderers;
using Markdig.Syntax;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

#pragma warning disable VSTHRD101 // Avoid unsupported async delegates

namespace MarkdownEditor
{
    public class Browser : IDisposable
    {
        private string _file;
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
        private const string MappedMarkdownEditorVirtualHostName = "markdown-editor-host";
        private const string MappedBrowsingFileVirtualHostName = "browsing-file-host";
        public bool HtmlTemplateLoaded { get; set; }
        public bool IsDarkTheme { get; set; }

        public WebView2 Control { get; private set; }

        public bool AutoSyncEnabled { get; set; } = MarkdownEditorPackage.Options.EnablePreviewSyncNavigation;

        private void InitBrowser()
        {
            Control = new WebView2();
            Control.HorizontalAlignment = HorizontalAlignment.Stretch;

            Control.Initialized += async (s, e) =>
            {
                try
                {
                    var tempDir = Path.Combine(Path.GetTempPath(), Assembly.GetExecutingAssembly().GetName().Name);
                    CoreWebView2EnvironmentOptions options = null;
                    var webView2Environment = await CoreWebView2Environment.CreateAsync(null, tempDir, options);
                    await Control.EnsureCoreWebView2Async(webView2Environment);
                    Control.CoreWebView2.SetVirtualHostNameToFolderMapping(MappedMarkdownEditorVirtualHostName, GetFolder(), CoreWebView2HostResourceAccessKind.Allow);
                    var baseHref = Path.GetDirectoryName(_file).Replace("\\", "/");
                    Control.CoreWebView2.SetVirtualHostNameToFolderMapping(MappedBrowsingFileVirtualHostName, baseHref, CoreWebView2HostResourceAccessKind.Allow);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, forceVisible: true);
                }
                try
                {
                    Zoom(_zoomFactor);
                    string result = await Control.ExecuteScriptAsync("document.body.offsetHeight;");
                    double.TryParse(result, out _cachedHeight);
                    await Control.ExecuteScriptAsync($@"document.documentElement.scrollTop={_positionPercentage * _cachedHeight / 100}");

                    await this.AdjustAnchors();
                }
                catch { }
            };

            // Open external links in default browser
            Control.NavigationStarting += async (s, e) =>
            {
                if (e.Uri == null)
                    return;

                var uri = new Uri(e.Uri);
                // If it's a file-based anchor we converted, open the related file if possible
                if (uri.Scheme == "about")
                {
                    string file = uri.LocalPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

                    if (file == "blank")
                    {
                        string fragment = uri.Fragment?.TrimStart('#');
                        await NavigateToFragment(fragment);
                        return;
                    }

                    if (!File.Exists(file))
                    {
                        string ext = null;

                        // If the file has no extension, see if one exists with a markdown extension.  If so,
                        // treat it as the file to open.
                        if (String.IsNullOrEmpty(Path.GetExtension(file)))
                            ext = ContentTypeDefinition.MarkdownExtensions.FirstOrDefault(fx => File.Exists(file + fx));

                        if (ext != null)
                            ProjectHelpers.OpenFileInPreviewTab(file + ext);
                    }
                    else
                        ProjectHelpers.OpenFileInPreviewTab(file);
                }
                else if (uri.IsAbsoluteUri && uri.Scheme.StartsWith("http"))
                {
                    e.Cancel = true;
                    Process.Start(e.Uri.ToString());
                }
            };
        }

        private async Task NavigateToFragment(string fragmentId)
        {
            try { await Control.ExecuteScriptAsync($"document.getElementById(\"{fragmentId}\").scrollIntoView(true)"); } catch { }
        }

        /// <summary>
        /// Adjust the file-based anchors so that they are navigable on the local file system
        /// </summary>
        /// <remarks>Anchors using the "file:" protocol appear to be blocked by security settings and won't work.
        /// If we convert them to use the "about:" protocol so that we recognize them, we can open the file in
        /// the <c>Navigating</c> event handler.</remarks>
        private async Task AdjustAnchors()
        {
            try
            {
                var script = @"for (const anchor of document.links) {
             if (anchor != null && anchor.protocol == 'file:') {
                 var pathName = null, hash = anchor.hash;
                 if (hash != null) {
                     pathName = anchor.pathname;
                     anchor.hash = null;
                     anchor.pathname = '';
                 }
                 anchor.protocol = 'about:';

                 if (hash != null) {
                     if (pathName == null || pathName.endsWith('/')) {
                         pathName = 'blank';
                     }
                     anchor.pathname = pathName;
                     anchor.hash = hash;
                 }
             }
         }";
                await Control.ExecuteScriptAsync(script.Replace("\r", "\\r").Replace("\n", "\\n"));
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

        public async Task UpdatePosition(int line)
        {
            if (HtmlTemplateLoaded && _currentDocument != null && AutoSyncEnabled)
            {
                _currentViewLine = _currentDocument.FindClosestLine(line);
                await SyncNavigation();
            }
        }

        private async Task SyncNavigation()
        {
            if (!HtmlTemplateLoaded)
            {
                return;
            }

            if (AutoSyncEnabled)
            {
                if (_currentViewLine == 0)
                {
                    // Forces the preview window to scroll to the top of the document
                    try { await Control.ExecuteScriptAsync("document.documentElement.scrollTop=0;"); } catch { }
                }
                else
                {
                    try { await Control.ExecuteScriptAsync($@"document.getElementById(""pragma-line-{_currentViewLine}"").scrollIntoView(true);"); } catch { }
                }
            }
            else
            {
                _currentViewLine = -1;
                try
                {
                    var result = await Control.ExecuteScriptAsync("document.documentElement.scrollTop;");
                    double.TryParse(result, out _cachedPosition);
                    result = await Control.ExecuteScriptAsync("document.body.offsetHeight;");
                    double.TryParse(result, out _cachedHeight);

                    _positionPercentage = _cachedPosition * 100 / _cachedHeight;
                }
                catch { }
            }
        }

        public async Task UpdateBrowser(ITextSnapshot snapshot)
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
                htmlRenderer.UseNonAsciiNoEscape = true;
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

            if (HtmlTemplateLoaded)
            {
                try
                {
                    html = html.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"");
                    await Control.ExecuteScriptAsync($@"document.getElementById(""___markdown-content___"").innerHTML=""{html}"";");
                }
                catch (Exception ex) { Logger.Log(ex); }

                // Makes sure that any code blocks get syntax highlighted by Prism
                try { await Control.ExecuteScriptAsync("Prism.highlightAll();"); } catch { }
                try { await Control.ExecuteScriptAsync("mermaid.init(undefined, document.querySelectorAll('.mermaid'));"); } catch { }
                try { await Control.ExecuteScriptAsync("if (typeof onMarkdownUpdate == 'function') onMarkdownUpdate();"); } catch { }

                // Adjust the anchors after and edit
                await this.AdjustAnchors();
            }
            else
            {
                var htmlTemplate = GetHtmlTemplate();
                html = string.Format(CultureInfo.InvariantCulture, "{0}", html);
                html = htmlTemplate.Replace("[content]", html);
                Logger.LogOnError(() => Control.NavigateToString(html));
                HtmlTemplateLoaded = true;
            }

            await SyncNavigation();
        }

        private async void OnStylesheetUpdated(object sender, EventArgs e)
        {
            if (HtmlTemplateLoaded)
            {
                var href = GetCustomStylesheet(_file) + "?" + new Guid();
                try { await Control.ExecuteScriptAsync($"document.styleSheets.item(0).href=\"{href}\""); } catch { }
            }
        }

        public static string GetFolder()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assembly);
        }

        private static string GetHtmlTemplateFileNameFromResource()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assembly);

            return Path.Combine(assemblyDir, "Resources\\md-template.html");
        }

        private static string GetHtmlTemplateFileName(string markdownFile)
        {
            if (!MarkdownEditorPackage.Options.EnablePreviewTemplate)
                return GetHtmlTemplateFileNameFromResource();

            var dir = new DirectoryInfo(Path.GetDirectoryName(markdownFile));
            var name = MarkdownEditorPackage.Options.HtmlTemplateFileName;

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

            return GetHtmlTemplateFileNameFromResource();
        }

        private string GetHtmlTemplate()
        {
            string cssHightlightFile;
            string mermaidJsParameters;
            if (IsDarkTheme)
            {
                cssHightlightFile = "highlight-dark.css";
                mermaidJsParameters = "{ 'securityLevel': 'loose', 'theme': 'dark', startOnLoad: true, flowchart: { htmlLabels: false } }";
            }
            else
            {
                cssHightlightFile = "highlight.css";
                mermaidJsParameters = "{ 'securityLevel': 'loose', 'theme': 'forest', startOnLoad: true, flowchart: { htmlLabels: false } }";
            }
            
            string cssHighlightPath = GetCustomStylesheet(_file) ?? $"http://{MappedMarkdownEditorVirtualHostName}/margin/{cssHightlightFile}";
            string scriptPrismPath = $"http://{MappedMarkdownEditorVirtualHostName}/margin/prism.js";
            string scriptMermaidPath = $"http://{MappedMarkdownEditorVirtualHostName}/margin/mermaid.min.js";

            var defaultHeadBeg = $@"
<head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=Edge"" />
    <meta charset=""utf-8"" />
    <base href=""http://{MappedBrowsingFileVirtualHostName}/"" />
    <link rel=""stylesheet"" href=""{cssHighlightPath}"" />
";
            var defaultContent = $@"
    <div id=""___markdown-content___"" class=""markdown-body"">
        [content]
    </div>
    <script src=""{scriptPrismPath}""></script>
    <script src=""{scriptMermaidPath}""></script>
    <script>
        mermaid.initialize({mermaidJsParameters});
    </script>
";

            var templateFileName = GetHtmlTemplateFileName(_file);
            var template = File.ReadAllText(templateFileName);
            return template
                .Replace("<head>", defaultHeadBeg)
                .Replace("[content]", defaultContent)
                .Replace("[title]", "Markdown Preview");
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

            Control.ZoomFactor = zoomFactor / 100;
        }

        public void Dispose()
        {
            if (Control != null)
                Control.Dispose();
        }
    }
}
