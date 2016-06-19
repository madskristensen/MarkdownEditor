using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideOptionPage(typeof(Options), "Text Editor\\Markdown", "General", 101, 111, true, new[] { "markdown", "md" }, ProvidesLocalizedCategoryName = false)]
    [Guid(_packageGuidString)]
    public sealed class MarkdownEditorPackage : Package
    {
        private const string _packageGuidString = "9ca64947-e9ca-4543-bfb8-6cce9be19fd6";
        private static Guid _guid = new Guid(_packageGuidString);

        public static Options Options { get; private set; }

        protected override void Initialize()
        {
            Options = (Options)GetDialogPage(typeof(Options));

            Logger.Initialize(this, Vsix.Name);
            base.Initialize();
        }
    }
}
