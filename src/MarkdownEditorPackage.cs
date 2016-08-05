using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor
{
    [Guid(PackageGuids.guidPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideLanguageService(typeof(MarkdownLanguage), MarkdownLanguage.LanguageName, 100, ShowDropDownOptions = true, DefaultToInsertSpaces = true, EnableCommenting = true, AutoOutlining = true)]
    [ProvideLanguageEditorOptionPage(typeof(Options), MarkdownLanguage.LanguageName, null, "Advanced", "#101", new[] { "markdown", "md" })]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".markdown")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".md")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mdown")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mdwn")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mkd")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mkdn")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".mmd")]
    [ProvideLanguageExtension(typeof(MarkdownLanguage), ".rst")]

    [ProvideEditorFactory(typeof(EditorFactory), 110, CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_OpensSlowly, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]

    [ProvideEditorExtension(typeof(EditorFactory), ".markdown", 0x32, NameResourceID = 110)]
    [ProvideEditorExtension(typeof(EditorFactory), ".md", 0x32, NameResourceID = 110)]
    [ProvideEditorExtension(typeof(EditorFactory), ".mdown", 0x32, NameResourceID = 110)]
    [ProvideEditorExtension(typeof(EditorFactory), ".mdwn", 0x32, NameResourceID = 110)]
    [ProvideEditorExtension(typeof(EditorFactory), ".mkd", 0x32, NameResourceID = 110)]
    [ProvideEditorExtension(typeof(EditorFactory), ".mkdn", 0x32, NameResourceID = 110)]
    [ProvideEditorExtension(typeof(EditorFactory), ".mmd", 0x32, NameResourceID = 110)]
    [ProvideEditorExtension(typeof(EditorFactory), ".rst", 0x32, NameResourceID = 110)]
    [ProvideEditorExtension(typeof(EditorFactory), ".*", 1, NameResourceID = 110)]
    public sealed class MarkdownEditorPackage : Package
    {
        private static Options _options;
        private static object _syncRoot = new object();
        private EditorFactory editorFactory;

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

        protected override void Initialize()
        {
            _options = (Options)GetDialogPage(typeof(Options));

            Logger.Initialize(this, Vsix.Name);
            ErrorList.Initialize(this);
            CopyAsHtmlCommand.Initialize(this);
            AddCustomStylesheet.Initialize(this);

            var serviceContainer = this as IServiceContainer;
            var langService = new MarkdownLanguage(this);
            serviceContainer.AddService(typeof(MarkdownLanguage), langService, true);

            editorFactory = new EditorFactory(this, typeof(MarkdownLanguage).GUID);
            RegisterEditorFactory(editorFactory);
        }

        private static void LoadPackage()
        {
            var shell = (IVsShell)GetGlobalService(typeof(SVsShell));

            IVsPackage package;

            if (shell.IsPackageLoaded(ref PackageGuids.guidPackage, out package) != VSConstants.S_OK)
                ErrorHandler.Succeeded(shell.LoadPackage(ref PackageGuids.guidPackage, out package));
        }
    }
}
