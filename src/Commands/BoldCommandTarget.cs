using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using System;

namespace MarkdownEditor
{
    internal class BoldCommandTarget : HotkeyCommandBase
    {
        private static readonly Guid _commandGroup = PackageGuids.guidPackageCmdSet;
        private static readonly uint _commandId = PackageIds.EditorBold;

        public BoldCommandTarget(IVsTextView adapter, IWpfTextView textView, ITextStructureNavigatorSelectorService navigator)
            : base(adapter, textView, navigator, _commandGroup, _commandId)
        { }

        public override string Symbol
        {
            get { return MarkdownEditorPackage.Options.BoldStyle == EmphasisStyle.Asterisk ? "**" : "__"; }
        }
    }
}