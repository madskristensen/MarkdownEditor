using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using Markdig.Renderers;
using Markdig.Syntax;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    /// <summary>
    /// The dropdown used to display the TOC and synchronize when the caret is moving
    /// </summary>
    /// <seealso cref="Microsoft.VisualStudio.Package.TypeAndMemberDropdownBars" />
    public class DropDownTocBars : TypeAndMemberDropdownBars
    {
        private readonly ITextView _textView;
        private MarkdownDocument _currentDocument;
        private List<HeadingBlock> _headings;

        private List<HeadingWrap> _members = null;
        private List<HeadingWrap> _previousMembersSync = null;
        private readonly LanguageService _languageService;

        public DropDownTocBars(LanguageService languageService, IVsTextView view)
        : base(languageService)
        {
            this._languageService = languageService;
            var componentModel = (IComponentModel)languageService.GetService(typeof(SComponentModel));
            var editorAdapterFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            this._textView = editorAdapterFactoryService.GetWpfTextView(view);

            var documentView = MarkdownDocumentView.Get(_textView);
            documentView.DocumentChanged += OnDocumentChanged;
            documentView.CaretChanged += OnCaretPositionChanged;

            UpdateElements(this._textView.TextSnapshot, false);
        }

        private void OnCaretPositionChanged(object sender, EventArgs e)
        {
            _languageService.SynchronizeDropdowns();
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
                    entries = (uint)_members.Count;
                    break;

                case ComboIndex.Members:
                    entries = 0;
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
            var localMembers = _members;
            switch (comboType)
            {
                case ComboIndex.Types:
                    text = localMembers?[entry].ToString() ?? string.Empty;
                    break;

                case ComboIndex.Members:
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
            selectedType = 0;
            selectedMember = 0;
            var localHeadings = _headings; // work on a copy of the variable
            if (localHeadings != null)
            {
                for (int i = localHeadings.Count - 1; i >= 0; i--)
                {
                    var heading = localHeadings[i];
                    if (line >= heading.Line)
                    {
                        selectedType = i + 1;
                        break;
                    }
                }
            }

            // Work on a copy
            var localMembers = _members;
            if (localMembers != null && localMembers != _previousMembersSync)
            {
                dropDownTypes.Clear();
                foreach (var wrap in localMembers)
                {
                    var textSpan = wrap.Heading != null
                        ? new TextSpan()
                        {
                            iStartIndex = 0,
                            iStartLine = wrap.Heading.Line,
                            iEndIndex = 0,
                            iEndLine = wrap.Heading.Line,
                        }
                        : new TextSpan();

                    dropDownTypes.Add(new DropDownMember(wrap.ToString(), textSpan, (int)ComboIndex.Types, DROPDOWNFONTATTR.FONTATTR_PLAIN));
                }
                _previousMembersSync = localMembers;
            }

            return true;
        }

        private void OnDocumentChanged(object sender, EventArgs textContentChangedEventArgs)
        {
            UpdateElements(_textView.TextSnapshot, true);
        }

        private void UpdateElements(ITextSnapshot snapshot, bool synchronize)
        {
            var doc = snapshot.ParseToMarkdown();
            if (doc == _currentDocument)
            {
                return;
            }

            _currentDocument = doc;
            _headings = doc.OfType<HeadingBlock>().ToList();

            var newMembers = new List<HeadingWrap> {new HeadingWrap("(top)")};

            var levels = new int[10];
            foreach (var heading in _headings)
            {
                if (heading.Level < levels.Length)
                {
                    levels[heading.Level]++;
                }
                for (int j = heading.Level + 1; j < levels.Length; j++)
                {
                    levels[j] = 0;
                }
                newMembers.Add(new HeadingWrap(heading, levels));
            }
            _members = newMembers;

            if (synchronize)
            {
                _languageService.SynchronizeDropdowns();
            }
        }

        private class HeadingWrap
        {
            private readonly string headingText;


            public HeadingWrap(string text)
            {
                headingText = text;
            }

            public HeadingWrap(HeadingBlock heading, int[] levels)
            {
                this.Heading = heading;
                headingText = ComputeHeadingText(heading, levels);
            }

            public HeadingBlock Heading { get; }

            public override string ToString()
            {
                return headingText;
            }

            /// <summary>
            /// Computes the heading text as displayed in the combo box.
            /// </summary>
            /// <param name="heading">The heading.</param>
            /// <param name="levels">The levels.</param>
            /// <returns>The heading text</returns>
            private static string ComputeHeadingText(HeadingBlock heading, int[] levels)
            {
                // This method takes into account the level of the heading, if it is already numbered or not.
                // If not numbered, it will compute an ordered number.

                // We start to indent at the very effective starting level (Someone may start the doc with a ### instead of #)
                int startIndent = 0;
                for (; startIndent <= heading.Level; startIndent++)
                {
                    if (levels[startIndent] != 0)
                    {
                        break;
                    }
                }

                var builder = new StringBuilder();
                for (int i = 1; i < heading.Level; i++)
                {
                    if (i >= startIndent)
                    {
                        builder.Append(' ');
                    }
                }

                var stringWriter = new StringWriter();
                var htmlRenderer = new HtmlRenderer(stringWriter) { EnableHtmlForInline = false };
                htmlRenderer.Render(heading.Inline);
                stringWriter.Flush();
                var headingText = stringWriter.ToString();

                // If the heading doesn't start by a digit, precalculate one
                if (headingText.Length == 0 || !char.IsDigit(headingText[0]))
                {
                    // If there are any intermediate levels not used, don't print them
                    // so # followed by ### is equivalent to # followed by ##
                    bool hasDigit = false;
                    bool hasDot = false;
                    for (int i = 1; i <= heading.Level; i++)
                    {
                        if (hasDigit && !hasDot)
                        {
                            builder.Append('.');
                            hasDot = true;
                        }

                        if (i < levels.Length && levels[i] != 0)
                        {
                            builder.Append(levels[i]);
                            hasDot = false;
                            hasDigit = true;
                        }
                    }

                    builder.Append(' ');
                }

                builder.Append(headingText);

                return builder.ToString();
            }
        }
    }
}
