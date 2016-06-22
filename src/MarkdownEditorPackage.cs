using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideEditorExtension(typeof(EditorFactory), ".md", 99)]
    [ProvideOptionPage(typeof(Options), "Text Editor\\Markdown", "General", 101, 111, true, new[] { "markdown", "md" }, ProvidesLocalizedCategoryName = false)]
    [Guid(PackageGuids.guidPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class MarkdownEditorPackage : Package
    {
        public static Options Options { get; private set; }

        protected override void Initialize()
        {
            Options = (Options)GetDialogPage(typeof(Options));

            Logger.Initialize(this, Vsix.Name);
            CopyAsHtmlCommand.Initialize(this);

            base.Initialize();
        }
    }
}
