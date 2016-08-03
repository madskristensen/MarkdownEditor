using System.ComponentModel.Composition;
using System.Windows.Threading;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(MarkdownLanguage.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class CommandRegistration : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        IClassifierAggregatorService ClassifierAggregatorService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
                ITextDocument document;

                if (!TextDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out document))
                    return;

                textView.Properties.GetOrCreateSingletonProperty(() => new PasteImage(textViewAdapter, textView, document.FilePath));
                textView.Properties.GetOrCreateSingletonProperty(() => new BoldCommandTarget(textViewAdapter, textView));
                textView.Properties.GetOrCreateSingletonProperty(() => new ItalicCommandTarget(textViewAdapter, textView));
                textView.Properties.GetOrCreateSingletonProperty(() => new InlineCodeCommandTarget(textViewAdapter, textView));
                textView.Properties.GetOrCreateSingletonProperty(() => new SmartIndentCommandTarget(textViewAdapter, textView));
                textView.Properties.GetOrCreateSingletonProperty(() => new IndentationCommandTarget(textViewAdapter, textView));
                textView.Properties.GetOrCreateSingletonProperty(() => new ToogleTaskCommandTarget(textViewAdapter, textView));
                textView.Properties.GetOrCreateSingletonProperty(() => new Navigate(textViewAdapter, textView));
            });
        }
    }
}