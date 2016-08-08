using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;
using Markdig;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor
{
    internal sealed class GenerateHtml
    {
        private readonly Package _package;
        private ProjectItem _item;

        private GenerateHtml(Package package)
        {
            _package = package;

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

            _item = ProjectHelpers.GetSelectedItem() as ProjectItem;
            if (_item == null || _item.ContainingProject == null)
                return;

            var project = _item.ContainingProject;

            string markdownFile = _item.FileNames[1];
            string ext = Path.GetExtension(markdownFile);

            if (!ContentTypeDefinition.MarkdownExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return;

            var htmlFile = GetHtmlFileName(markdownFile);

            button.Checked = File.Exists(htmlFile);
            button.Visible = button.Enabled = true;
        }

        private void Execute(object sender, EventArgs e)
        {
            string markdownFile = _item.FileNames[1];
            string htmlFile = GetHtmlFileName(markdownFile);

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

            string htmlFile = GetHtmlFileName(markdownFile);

            File.WriteAllText(htmlFile, html, new UTF8Encoding(true));
            ProjectHelpers.AddNestedFile(markdownFile, htmlFile);
        }

        private static string GetHtmlFileName(string markdownFile)
        {
            return Path.ChangeExtension(markdownFile, ".html");
        }
    }
}
