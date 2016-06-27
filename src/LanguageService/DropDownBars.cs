using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    public class DropDownBars : TypeAndMemberDropdownBars
    {
        private readonly IList<string> declarations;

        private readonly IList<string> members;

        public DropDownBars(LanguageService languageService, IVsTextView view)
        : base(languageService)
        {
            IVsTextLines lines;
            ErrorHandler.ThrowOnFailure(view.GetBuffer(out lines));

            // TODO: initialize declarations and members from the given text view...
            declarations = new List<string> { "Current document" };
            members = new List<string> { "foo", "bar" };
        }

        private enum ComboIndex
        {
            Types = 0,
            Members = 1
        }

        public override int GetComboAttributes(
            int combo,
            out uint entries,
            out uint entryType,
            out IntPtr imageList)
        {
            entries = 0;
            imageList = IntPtr.Zero;

            entryType = (uint)DROPDOWNENTRYTYPE.ENTRY_TEXT;

            var comboType = (ComboIndex)combo;
            switch (comboType)
            {
                case ComboIndex.Types:
                    entries = (uint)this.declarations.Count();
                    break;

                case ComboIndex.Members:
                    entries = (uint)this.members.Count();
                    break;
            }

            return VSConstants.S_OK;
        }

        public override int GetEntryAttributes(
            int combo,
            int entry,
            out uint fontAttrs)
        {
            fontAttrs = (uint)DROPDOWNFONTATTR.FONTATTR_PLAIN;

            return VSConstants.S_OK;
        }

        public override int GetEntryText(
            int combo,
            int entry,
            out string text)
        {
            text = null;
            var comboType = (ComboIndex)combo;
            switch (comboType)
            {
                case ComboIndex.Types:
                    text = this.declarations[entry];
                    break;

                case ComboIndex.Members:
                    text = this.members[entry];
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
