using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;

namespace MarkdownEditor
{
    [Guid("ecf70314-91e6-490d-8ea3-45e82b2d28e9")]
    public class MarkdownLanguage : LanguageService
    {
        public const string LanguageName = "Markdown";
        private LanguagePreferences preferences = null;

        public MarkdownLanguage(object site)
        {
            SetSite(site);
        }

        public override Source CreateSource(IVsTextLines buffer)
        {
            return new MarkdownSource(this, buffer, new MarkdownColorizer(this, buffer, null));
        }

        public override TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView forView)
        {
            return new DropDownTocBars(this, forView);
        }

        public override LanguagePreferences GetLanguagePreferences()
        {
            if (preferences == null)
            {
                preferences = new LanguagePreferences(Site, typeof(MarkdownLanguage).GUID, Name);

                if (preferences != null)
                {
                    preferences.Init();

                    preferences.EnableCodeSense = true;
                    preferences.EnableMatchBraces = true;
                    preferences.EnableMatchBracesAtCaret = true;
                    preferences.EnableShowMatchingBrace = true;
                    preferences.EnableCommenting = true;
                    preferences.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES;
                    preferences.LineNumbers = false;
                    preferences.MaxErrorMessages = 100;
                    preferences.AutoOutlining = false;
                    preferences.MaxRegionTime = 2000;
                    preferences.ShowNavigationBar = true;
                    preferences.InsertTabs = false;
                    preferences.IndentSize = 2;
                    preferences.ShowNavigationBar = true;

                    preferences.WordWrap = true;
                    preferences.WordWrapGlyphs = true;

                    preferences.AutoListMembers = true;
                    preferences.EnableQuickInfo = true;
                    preferences.ParameterInformation = true;
                }
            }

            return preferences;
        }

        public override IScanner GetScanner(IVsTextLines buffer)
        {
            return null;
        }

        public override AuthoringScope ParseSource(ParseRequest req)
        {
            return new MardownAuthoringScope(req);
        }

        public override string GetFormatFilterList()
        {
            return "Markdown File (*.markdown, *.md, *.mdown, *.mdwn, *.mkd, *.mkdn, *.mmd)|*.markdown;*.md;*.mdown;*.mdwn;*.mdwn;*.mkd;*.mkdn;*.mmd";
        }

        public override string Name => LanguageName;

        public override void Dispose()
        {
            try
            {
                if (preferences != null)
                {
                    preferences.Dispose();
                    preferences = null;
                }
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
