using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    public class MarkdownContentTypeDefinition
    {
        public const string MarkdownContentType = "Markdown2";

        [Export(typeof(ContentTypeDefinition))]
        [Name(MarkdownContentType)]
        [BaseDefinition("code")]
        public ContentTypeDefinition IMarkdownContentType { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(MarkdownContentType)]
        [FileExtension(".markdown")]
        public FileExtensionToContentTypeDefinition GitFileExtension { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(MarkdownContentType)]
        [FileExtension(".mdown")]
        public FileExtensionToContentTypeDefinition TfFileExtension { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(MarkdownContentType)]
        [FileExtension(".md")]
        public FileExtensionToContentTypeDefinition HgFileExtension { get; set; }

        [Export(typeof(FileExtensionToContentTypeDefinition))]
        [ContentType(MarkdownContentType)]
        [FileExtension(".mdwn")]
        public FileExtensionToContentTypeDefinition NodemonFileExtension { get; set; }
    }
}
