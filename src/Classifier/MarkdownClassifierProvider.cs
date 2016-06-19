using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    internal class MarkdownClassifierProvider : IClassifierProvider
    {
        [Import]
        private IClassificationTypeRegistryService classificationRegistry { get; set; }
        
        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new MarkdownClassifier(buffer, classificationRegistry));
        }
    }
}
