using System.ComponentModel.Composition;
using System.Windows.Threading;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
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
        IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        IClassifierAggregatorService ClassifierAggregatorService { get; set; }

        [Import]
        ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
                ITextDocument document = null;

                if (textView == null || !TextDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out document))
                    return;

                textView.Properties.GetOrCreateSingletonProperty(() => new PasteImage(textViewAdapter, textView, document.FilePath));
                textView.Properties.GetOrCreateSingletonProperty(() => new BoldCommandTarget(textViewAdapter, textView, NavigatorService));
                textView.Properties.GetOrCreateSingletonProperty(() => new ItalicCommandTarget(textViewAdapter, textView, NavigatorService));
                textView.Properties.GetOrCreateSingletonProperty(() => new InlineCodeCommandTarget(textViewAdapter, textView, NavigatorService));
                textView.Properties.GetOrCreateSingletonProperty(() => new SmartIndentCommandTarget(textViewAdapter, textView));
                textView.Properties.GetOrCreateSingletonProperty(() => new IndentationCommandTarget(textViewAdapter, textView));
                textView.Properties.GetOrCreateSingletonProperty(() => new ToogleTaskCommandTarget(textViewAdapter, textView));
                textView.Properties.GetOrCreateSingletonProperty(() => new Navigate(textViewAdapter, textView));
                textView.Properties.GetOrCreateSingletonProperty(() => new FormatTableCommandTarget(textViewAdapter, textView, NavigatorService));


                document.FileActionOccurred += Document_FileActionOccurred;
            });
        }

        private void Document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                if (GenerateHtml.HtmlGenerationEnabled(e.FilePath))
                    GenerateHtml.GenerateHtmlFile(e.FilePath);
            }
        }
    }
}