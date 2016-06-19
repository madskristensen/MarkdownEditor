using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class WebEditorsViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            ITextDocument document;

            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out document))
            {
                textView.Properties.GetOrCreateSingletonProperty(() => new PasteImage(textViewAdapter, textView, document.FilePath));
            }
        }
    }
}