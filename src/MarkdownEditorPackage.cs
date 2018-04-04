using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace MarkdownEditor
{
    [Guid(PackageGuids.guidPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideLanguageService(typeof(MarkdownLanguage), MarkdownLanguage.LanguageName, 100, ShowDropDownOptions = true, DefaultToInsertSpaces = true, EnableCommenting = true, AutoOutlining = true, MatchBraces = true, MatchBracesAtCaret = true, ShowMatchingBrace = true, ShowSmartIndent = true)]
    [ProvideLanguageEditorOptionPage(typeof(Options), MarkdownLanguage.LanguageName, null, "Advanced", "#101", new[] { "markdown", "md" })]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".markdown")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".md")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mdown")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mdwn")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mkd")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mkdn")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mmd")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".rst")]

    [ProvideEditorFactory(typeof(EditorFactory), 110, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_None, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]

    [ProvideEditorExtension(typeof(EditorFactory), ".markdown", 1000)]
    [ProvideEditorExtension(typeof(EditorFactory), ".md", 1000)]
    [ProvideEditorExtension(typeof(EditorFactory), ".mdown", 1000)]
    [ProvideEditorExtension(typeof(EditorFactory), ".mdwn", 1000)]
    [ProvideEditorExtension(typeof(EditorFactory), ".mkd", 1000)]
    [ProvideEditorExtension(typeof(EditorFactory), ".mkdn", 1000)]
    [ProvideEditorExtension(typeof(EditorFactory), ".mmd", 1000)]
    [ProvideEditorExtension(typeof(EditorFactory), ".rst", 1000)]
    [ProvideEditorExtension(typeof(EditorFactory), ".*", 2, NameResourceID = 110)]

    [ProvideBraceCompletion(MarkdownLanguage.LanguageName)]

    //[ProvideAutoLoad("559ffa40-ab05-4ca2-9cc6-fb20c4a37112", PackageAutoLoadFlags.BackgroundLoad)]
    //[ProvideUIContextRule("559ffa40-ab05-4ca2-9cc6-fb20c4a37112",
    //name: Vsix.Name,
    //expression: "(markdown | md | mdown | mdwn | mkd | mmd | rst)",
    //termNames: new[] { "markdown", "md", "mdown", "mdwn", "mkd", "mkdn", "mmd", "rst" },
    //termValues: new[] {
    //    "HierSingleSelectionName:.markdown$",
    //    "HierSingleSelectionName:.md$",
    //    "HierSingleSelectionName:.mdown$",
    //    "HierSingleSelectionName:.mdwn$",
    //    "HierSingleSelectionName:.mkd$",
    //    "HierSingleSelectionName:.mkdn$",
    //    "HierSingleSelectionName:.mmd$",
    //    "HierSingleSelectionName:.rst$"})]
    public sealed class MarkdownEditorPackage : AsyncPackage
    {
        private static Options _options;
        private static object _syncRoot = new object();

        public static Options Options
        {
            get
            {
                if (_options == null)
                {
                    lock (_syncRoot)
                    {
                        if (_options == null)
                        {
                            LoadPackage();
                        }
                    }
                }

                return _options;
            }
        }

        public static MarkdownLanguage Language
        {
            get;
            private set;
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Language = new MarkdownLanguage(this);
        
            var editorFactory = new EditorFactory(this, typeof(MarkdownLanguage).GUID);
            RegisterEditorFactory(editorFactory);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            _options = (Options)GetDialogPage(typeof(Options));

            Logger.Initialize(this, Vsix.Name);
            ErrorList.Initialize(this);
            CopyAsHtmlCommand.Initialize(this);
            AddCustomStylesheet.Initialize(this);
            GenerateHtml.Initialize(this);
        }

        private static void LoadPackage()
        {
            var shell = (IVsShell)GetGlobalService(typeof(SVsShell));

            if (shell.IsPackageLoaded(ref PackageGuids.guidPackage, out IVsPackage package) != VSConstants.S_OK)
                ErrorHandler.Succeeded(shell.LoadPackage(ref PackageGuids.guidPackage, out package));
        }
    }
}
