using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    public class ContentTypeDefinition
    {
        public static string[] MarkdownExtensions = { ".markdown", ".md", ".mdown", ".mdwn", ".mkd", ".mkdn", ".mmd", ".rst" };

        [Export(typeof(ContentTypeDefinition))]
        [Name(MarkdownLanguage.LanguageName)]
        [BaseDefinition("text")]
        public ContentTypeDefinition MarkdownContentType { get; set; }
    }
}
