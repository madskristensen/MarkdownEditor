using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    [Export(typeof(IClassifierProvider))]
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(MarkdownContentTypeDefinition.MarkdownContentType)]
    internal class MarkdownClassifierProvider : IClassifierProvider, ITaggerProvider
    {
        [Import]
        private IClassificationTypeRegistryService classificationRegistry { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new MarkdownClassifier(buffer, classificationRegistry)) as ITagger<T>;
        }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new MarkdownClassifier(buffer, classificationRegistry));
        }
    }
}
