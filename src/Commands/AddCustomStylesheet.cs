using System;
using System.ComponentModel.Design;
using System.IO;
using System.Windows;
using EnvDTE;
using Markdig;
using Microsoft.VisualStudio.Shell;

namespace MarkdownEditor
{
    internal sealed class AddCustomStylesheet
    {
        private readonly Package _package;

        private AddCustomStylesheet(Package package)
        {
            _package = package;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var cmd = new CommandID(PackageGuids.guidPackageCmdSet, PackageIds.AddCustomStylesheet);
                var menuItem = new OleMenuCommand(Execute, cmd);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        public static AddCustomStylesheet Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new AddCustomStylesheet(package);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            var document = ProjectHelpers.DTE.ActiveDocument;
            var item = ProjectHelpers.DTE.Solution.FindProjectItem(document.FullName);

            if (item?.ContainingProject == null)
                return;

            var destFile = GetStylesheetLocation(document.FullName);

            if (File.Exists(destFile))
                return;

            if (MarkdownLanguage.LanguageName.Equals(document?.Language, StringComparison.OrdinalIgnoreCase))
            {
                button.Visible = button.Enabled = true;
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            var msg = "This will create a .css file in the same directory as active markdown document.\r\rDo you wish to continue?";
            var answer = MessageBox.Show(msg, Vsix.Name, MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (answer == MessageBoxResult.Cancel)
                return;

            string document = ProjectHelpers.DTE.ActiveDocument.FullName;
            string extensionFolder = Browser.GetFolder();
            string srcFile = Path.Combine(extensionFolder, "margin\\highlight.css");
            string destFile = GetStylesheetLocation(document);

            File.Copy(srcFile, destFile);

            var item = ProjectHelpers.DTE.Solution.FindProjectItem(document);

            if (item?.ContainingProject != null)
            {
                item.ContainingProject.AddFileToProject(destFile);

                ProjectHelpers.DTE.ItemOperations.OpenFile(destFile);
                ProjectHelpers.DTE.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
                ProjectHelpers.DTE.ActiveDocument.Activate();
            }
        }

        private static string GetStylesheetLocation(string document)
        {
            string dir = Path.GetDirectoryName(document);
            return Path.Combine(dir, MarkdownEditorPackage.Options.CustomStylesheetFileName);
        }
    }
}
