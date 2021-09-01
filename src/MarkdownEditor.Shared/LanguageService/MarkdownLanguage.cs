using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;

namespace MarkdownEditor
{
    [Guid("ecf70314-91e6-490d-8ea3-45e82b2d28e9")]
    public class MarkdownLanguage : LanguageService
    {
        public const string LanguageName = "Markdown";
        private LanguagePreferences _preferences = null;

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
            if (_preferences.ShowNavigationBar)
            {
                return new DropDownTocBars(this, forView);
            }
            else
            {
                return base.CreateDropDownHelper(forView);
            }
        }

        public override LanguagePreferences GetLanguagePreferences()
        {
            if (_preferences == null)
            {
                _preferences = new LanguagePreferences(Site, typeof(MarkdownLanguage).GUID, Name);

                if (_preferences != null)
                {
                    _preferences.Init();

                    _preferences.EnableCodeSense = true;
                    _preferences.EnableMatchBraces = true;
                    _preferences.EnableMatchBracesAtCaret = true;
                    _preferences.EnableShowMatchingBrace = true;
                    _preferences.EnableCommenting = true;
                    _preferences.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES;
                    _preferences.LineNumbers = false;
                    _preferences.MaxErrorMessages = 100;
                    _preferences.AutoOutlining = false;
                    _preferences.MaxRegionTime = 2000;
                    _preferences.InsertTabs = false;
                    _preferences.IndentSize = 2;
                    _preferences.IndentStyle = IndentingStyle.Smart;

                    _preferences.WordWrap = true;
                    _preferences.WordWrapGlyphs = true;

                    _preferences.AutoListMembers = true;
                    _preferences.EnableQuickInfo = true;
                    _preferences.ParameterInformation = true;
                }
            }

            return _preferences;
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
                if (_preferences != null)
                {
                    _preferences.Dispose();
                    _preferences = null;
                }
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
