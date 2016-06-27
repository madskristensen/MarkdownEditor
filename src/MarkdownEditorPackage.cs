using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor
{
    [Guid(PackageGuids.guidPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideService(typeof(MarkdownLanguage), ServiceName = MarkdownLanguage.LanguageName)]
    [ProvideLanguageService(typeof(MarkdownLanguage), MarkdownLanguage.LanguageName, 100, ShowDropDownOptions = true, DefaultToInsertSpaces = true, EnableCommenting = true, AutoOutlining = true)]
    [ProvideLanguageEditorOptionPage(typeof(Options), MarkdownLanguage.LanguageName, null, "Advanced", "#101", new[] { "markdown", "md" })]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".markdown")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".md")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mdown")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mdwn")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mkd")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mkdn")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mmd")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class MarkdownEditorPackage : Package
    {
        public static Options Options { get; private set; }

        protected override void Initialize()
        {
            Options = (Options)GetDialogPage(typeof(Options));

            var serviceContainer = this as IServiceContainer;
            var langService = new MarkdownLanguage();
            langService.SetSite(this);
            serviceContainer.AddService(typeof(MarkdownLanguage), langService, true);

            Logger.Initialize(this, Vsix.Name);
            CopyAsHtmlCommand.Initialize(this);

            base.Initialize();
        }
    }
}
