using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(MarkdownLanguage.LanguageName)]
    internal class MarkdownClassifierProvider : IClassifierProvider
    {
        [Import]
        private IClassificationTypeRegistryService classificationRegistry { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            ITextDocument document;

            if (!TextDocumentFactoryService.TryGetTextDocument(buffer, out document))
                return null;

            return buffer.Properties.GetOrCreateSingletonProperty(() => new MarkdownClassifier(buffer, classificationRegistry, document.FilePath));
        }
    }
}
