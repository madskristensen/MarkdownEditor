using System;
using Markdig.Syntax.Inlines;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace MarkdownEditor
{
    class LinkError : Error
    {
        public string LinkUrl { get; private set; }

        public static Error Create(string file, LinkInline link)
        {
            return new LinkError
            {
                File = file,
                Message = $"The file \"{link.Url}\" could not be resolved.",
                Line = link.Line,
                Column = link.Column,
                ErrorCode = "missing-file",
                // FIX: There seems to be something wrong with the Markdig parser
                //      when parsing a referenced image e.g.
                //      ![The image][image]
                //      ^^^^^^^^^^^^^~~~~~^
                //      [image]: images/the-image.png
                //
                //      The link.Reference.UrlSpan doesn't have correct values
                //      which forces us to use this code
                Span = link.Reference == null && link.UrlSpan.HasValue
                                ? new Span(link.UrlSpan.Value.Start, link.UrlSpan.Value.Length)
                                : new Span(link.Span.Start, link.Span.Length),
                LinkUrl = link.Url
            };
        }

        public override IErrorTag CreateTag()
        {
            return new LinkErrorTag(Message) { Url = LinkUrl };
        }
    }

    class LinkErrorTag : ErrorTag
    {
        public static readonly string ERROR_TYPE = "Intellisense-markdown-link-error";

        public string Url { get; set; }
        public LinkErrorTag(object toolTipContent) : base(ERROR_TYPE, toolTipContent)
        {

        }
    }
}