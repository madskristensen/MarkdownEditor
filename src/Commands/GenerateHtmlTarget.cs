using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using EnvDTE;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor
{
    internal sealed class GenerateHtml
    {
        private readonly Package _package;
        private ProjectItem _item;
        private string htmlExtension = ".html";

        private GenerateHtml(Package package)
        {
            _package = package;
            
            htmlExtension = MarkdownEditorPackage.Options.HTMLFileExtension;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var cmd = new CommandID(PackageGuids.guidPackageCmdSet, PackageIds.GenerateHtml);
                var menuItem = new OleMenuCommand(Execute, cmd);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        public static GenerateHtml Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new GenerateHtml(package);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            if (!MarkdownEditorPackage.Options.GenerateHtmlFiles)
                return;

            _item = ProjectHelpers.GetSelectedItem() as ProjectItem;
            if (_item == null || _item.ContainingProject == null)
                return;

            var project = _item.ContainingProject;

            string markdownFile = _item.FileNames[1];
            string ext = Path.GetExtension(markdownFile);

            if (!ContentTypeDefinition.MarkdownExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return;

            var htmlFile = GetHtmlFileName(markdownFile, htmlExtension);

            button.Checked = File.Exists(htmlFile);
            button.Visible = button.Enabled = true;
        }

        private void Execute(object sender, EventArgs e)
        {
            string markdownFile = _item.FileNames[1];
            string htmlFile = GetHtmlFileName(markdownFile, htmlExtension);

            if (File.Exists(htmlFile))
            {
                string msg = "This will delete the .html file from your project.\r\rDo you wish to continue?";
                var answer = VsShellUtilities.ShowMessageBox(_package, msg, Vsix.Name, OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                if (answer == (int)VSConstants.MessageBoxResult.IDOK)
                    ProjectHelpers.DeleteFileFromProject(htmlFile);
            }
            else
            {
                GenerateHtmlFile(markdownFile);
            }
        }

        public static void GenerateHtmlFile(string markdownFile)
        {
            string content = File.ReadAllText(markdownFile);
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var html = Markdown.ToHtml(content, pipeline).Replace("\n", Environment.NewLine);

            string htmlFileName = GetHtmlFileName(markdownFile, htmlExtension);
            html = CreateFromHtmlTemplate(markdownFile, content, html);

            File.WriteAllText(htmlFileName, html, new UTF8Encoding(true));
            ProjectHelpers.AddNestedFile(markdownFile, htmlFileName);
        }

        private static string CreateFromHtmlTemplate(string markdownFile, string content, string html)
        {
            try
            {
                string templateFileName = GetHtmlTemplate(markdownFile);
                string template = File.ReadAllText(templateFileName);

                var doc = Markdown.Parse(content);
                string title = GetTitle(markdownFile, doc);

                return template.Replace("[title]", title).Replace("[content]", html);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return html;
            }
        }

        private static string GetTitle(string markdownFile, MarkdownDocument doc)
        {
            var inline = doc.Descendants().OfType<HeadingBlock>().FirstOrDefault().Inline;
            string title = Path.GetFileNameWithoutExtension(markdownFile);

            using (var stringWriter = new StringWriter())
            {
                try
                {
                    var htmlRenderer = new HtmlRenderer(stringWriter) { EnableHtmlForInline = false };
                    htmlRenderer.Render(inline);
                    stringWriter.Flush();
                    title = stringWriter.ToString();
                }
                catch
                { }
            }

            return title;
        }

        private static string GetHtmlTemplate(string markdownFile)
        {
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

            string assembly = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assembly);

            return Path.Combine(assemblyDir, "Resources\\md-template.html");
        }

        public static bool HtmlGenerationEnabled(string markdownFile)
        {
            string htmlFile = GetHtmlFileName(markdownFile, htmlExtension);

            return File.Exists(htmlFile);
        }

        private static string GetHtmlFileName(string markdownFile)
        {
            return Path.ChangeExtension(markdownFile, ".html");
        }
        
         private static string GetHtmlFileName(string markdownFile, string extension)
        {
            return Path.ChangeExtension(markdownFile, extension);
        }
    }
}
