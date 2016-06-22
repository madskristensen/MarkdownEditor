using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    public class MarkdownContentTypeDefinition
    {
        public const string MarkdownContentType = "Markdown";

        [Export(typeof(ContentTypeDefinition))]
        [Name(MarkdownContentType)]
        [BaseDefinition("code")]
        public ContentTypeDefinition IMarkdownContentType { get; set; }
    }
}
