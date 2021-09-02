using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;


namespace MarkdownEditor.Outlining
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(MarkdownLanguage.LanguageName)]
    public class MarkdownOutliningProvider : ITaggerProvider
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ITextDocument document;

            if (!TextDocumentFactoryService.TryGetTextDocument(buffer, out document))
                return null;

            return buffer.Properties.GetOrCreateSingletonProperty(() => new MarkdownOutliningTagger(buffer, document.FilePath)) as ITagger<T>;
        }
    }
}
