using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("CSS")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class CssCreationListener : IVsTextViewCreationListener
    {
        [Import]
        IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            ITextDocument document;

            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out document))
            {
                document.FileActionOccurred += DocumentSaved;
            }
        }

        private void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentLoadedFromDisk)
                return;

            string fileName = Path.GetFileName(e.FilePath);

            if (fileName == MarkdownEditorPackage.Options.CustomStylesheetFileName)
                StylesheetUpdated?.Invoke(this, EventArgs.Empty);
        }

        public static event EventHandler<EventArgs> StylesheetUpdated;
    }
}
