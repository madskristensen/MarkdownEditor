using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class ItalicCommandTarget : HotkeyCommandBase
    {
        private static readonly Guid _commandGroup = typeof(VSConstants.VSStd2KCmdID).GUID;
        private static readonly uint _commandId = (uint)VSConstants.VSStd2KCmdID.ISEARCH; // maps to Edit.IncrementalSearch

        public ItalicCommandTarget(IVsTextView adapter, IWpfTextView textView, ITextStructureNavigatorSelectorService navigator)
            :base (adapter, textView, navigator, _commandGroup, _commandId)
        { }

        public override string Symbol
        {
            get { return MarkdownEditorPackage.Options.BoldStyle == EmphasisStyle.Asterisk ? "*" : "_"; }
        }
    }
}