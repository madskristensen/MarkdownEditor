using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Tagging;

namespace MarkdownEditor
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Markdown Suggested Actions")]
    [ContentType(MarkdownLanguage.LanguageName)]
    class SuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        //[Import]
        ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        //[Import]
        IViewTagAggregatorFactoryService ViewTagAggregatorFactoryService { get; set; }

        [ImportingConstructor]
        public SuggestedActionsSourceProvider(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService, ITextDocumentFactoryService textDocumentFactoryService)
        {
            ViewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
            this.TextDocumentFactoryService = textDocumentFactoryService;

        }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            ITextDocument document;

            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out document))
            {
                return textView.Properties.GetOrCreateSingletonProperty(() => new SuggestedActionsSource(textView, document.FilePath) { TagService = ViewTagAggregatorFactoryService });
            }

            return null;
        }
    }
}
