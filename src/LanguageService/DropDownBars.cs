using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Markdig.Syntax;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    public class DropDownBars : TypeAndMemberDropdownBars
    {
        private IList<string> _declarations = new List<string> { "Current document" };

        private IList<string> _members = new List<string> { "foo", "bar" };

        public DropDownBars(LanguageService languageService, IVsTextView view)
        : base(languageService)
        {
            IVsTextLines lines;
            ErrorHandler.ThrowOnFailure(view.GetBuffer(out lines));

            int lineCount;
            ErrorHandler.ThrowOnFailure(lines.GetLineCount(out lineCount));

            string text;
            ErrorHandler.ThrowOnFailure(lines.GetLineText(0, 0, lineCount - 1, 0, out text));

            var doc = Markdig.Parsers.MarkdownParser.Parse(text); // TODO: use MarkdownFactory

            var children = doc.Descendants().OfType<HeadingBlock>();
            _members = children.Select(heading => text.Substring(heading.Span.Start, heading.Span.Length)).ToList();
        }
        private enum ComboIndex
        {
            Types = 0,
            Members = 1
        }

        public override int GetComboAttributes(int combo, out uint entries, out uint entryType, out IntPtr imageList)
        {
            entries = 0;
            imageList = IntPtr.Zero;

            entryType = (uint)DROPDOWNENTRYTYPE.ENTRY_TEXT;

            var comboType = (ComboIndex)combo;
            switch (comboType)
            {
                case ComboIndex.Types:
                    entries = (uint)_declarations.Count();
                    break;

                case ComboIndex.Members:
                    entries = (uint)_members.Count();
                    break;
            }

            return VSConstants.S_OK;
        }

        public override int GetEntryAttributes(int combo, int entry, out uint fontAttrs)
        {
            fontAttrs = (uint)DROPDOWNFONTATTR.FONTATTR_PLAIN;

            return VSConstants.S_OK;
        }

        public override int GetEntryText(int combo, int entry, out string text)
        {
            text = null;
            var comboType = (ComboIndex)combo;
            switch (comboType)
            {
                case ComboIndex.Types:
                    text = _declarations[entry];
                    break;

                case ComboIndex.Members:
                    text = _members[entry];
                    break;
            }

            return VSConstants.S_OK;
        }

        public override bool OnSynchronizeDropdowns(
            LanguageService languageService,
            IVsTextView textView,
            int line,
            int col,
            ArrayList dropDownTypes,
            ArrayList dropDownMembers,
            ref int selectedType,
            ref int selectedMember)
        {

            return false;
        }
    }
}
