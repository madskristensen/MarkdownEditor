using System;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class BoldCommandTarget : HotkeyCommandBase
    {
        private static readonly Guid _commandGroup = new Guid("{c9dd4a59-47fb-11d2-83e7-00c04f9902c1}");
        private static readonly uint _commandId = 311; // maps to Debug.FunctionalBreakpoint

        public BoldCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, _commandGroup, _commandId)
        { }

        public override string Symbol
        {
            get { return MarkdownEditorPackage.Options.BoldStyle == EmphasisStyle.Asterisk ? "**" : "__"; }
        }
    }
}