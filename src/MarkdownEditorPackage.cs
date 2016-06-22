using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor
{
    [Guid(PackageGuids.guidPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]

    [ProvideService(typeof(MarkdownLanguage), ServiceName = MarkdownContentTypeDefinition.MarkdownContentType)]
    [ProvideLanguageService(typeof(MarkdownLanguage), MarkdownContentTypeDefinition.MarkdownContentType, 100, DefaultToInsertSpaces = true, EnableCommenting = true, MatchBraces = true, MatchBracesAtCaret = true, ShowMatchingBrace = true, AutoOutlining = true)]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".markdown")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".md")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mdown")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mdwn")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mkd")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mkdn")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mmd")]

    [ProvideOptionPage(typeof(Options), "Text Editor\\Markdown", "Advanced", 101, 111, true, new[] { "markdown", "md" }, ProvidesLocalizedCategoryName = false)]
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
