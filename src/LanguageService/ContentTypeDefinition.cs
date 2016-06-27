using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    public class ContentTypeDefinition
    {
        [Export(typeof(ContentTypeDefinition))]
        [Name(MarkdownLanguage.LanguageName)]
        [BaseDefinition("text")]
        public ContentTypeDefinition MarkdownContentType { get; set; }
    }
}
