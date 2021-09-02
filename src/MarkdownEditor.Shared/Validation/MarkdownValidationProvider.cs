using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;


namespace MarkdownEditor.Outlining
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType(MarkdownLanguage.LanguageName)]
    public class MarkdownValidationProvider : ITaggerProvider
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITextDocument document;

            if (!TextDocumentFactoryService.TryGetTextDocument(buffer, out document))
                return null;

            return buffer.Properties.GetOrCreateSingletonProperty(() => new MarkdownValidationTagger(buffer, document.FilePath)) as ITagger<T>;
        }
    }
}
