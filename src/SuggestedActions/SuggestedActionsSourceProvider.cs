using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Markdown Suggested Actions")]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    class SuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        [Import]
        ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            ITextDocument document;

            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out document))
            {
                return textView.Properties.GetOrCreateSingletonProperty(() => new SuggestedActionsSource(textView, document.FilePath));
            }

            return null;
        }
    }
}
