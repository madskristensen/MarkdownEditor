using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class InlineCodeCommandTarget : HotkeyCommandBase
    {
        private static readonly Guid _commandGroup = PackageGuids.guidPackageCmdSet;
        private static readonly uint _commandId = PackageIds.EditorInlineCode;

        public InlineCodeCommandTarget(IVsTextView adapter, IWpfTextView textView, ITextStructureNavigatorSelectorService navigator)
            : base(adapter, textView, navigator, _commandGroup, _commandId)
        { }

        public override string Symbol
        {
            get { return "`"; }
        }

        public override string MultiLineSymbolStart
        {
            get { return $"```{Environment.NewLine}"; }
        }

        public override string MultiLineSymbolEnd
        {
            get { return $"{Environment.NewLine}```"; }
        }
    }
}