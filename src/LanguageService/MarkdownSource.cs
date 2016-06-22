using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class MarkdownSource : Source
    {
        public MarkdownSource(LanguageService service, IVsTextLines textLines, Colorizer colorizer)
            : base(service, textLines, colorizer)
        { }

        public override CommentInfo GetCommentFormat()
        {
            return new CommentInfo
            {
                UseLineComments = false,
                BlockStart = "<!--",
                BlockEnd = "-->"
            };
        }
    }
}

