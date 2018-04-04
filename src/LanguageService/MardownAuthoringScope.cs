using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.IO;
using System.Linq;

namespace MarkdownEditor
{
    internal class MardownAuthoringScope : AuthoringScope
    {
        private ParseRequest _req;

        public MardownAuthoringScope(ParseRequest req)
        {
            _req = req;
        }

        public override string GetDataTipText(int line, int col, out TextSpan span)
        {
            span = new TextSpan();
            return null;
        }

        public override Declarations GetDeclarations(IVsTextView view, int line, int col, TokenInfo info, ParseReason reason)
        {
            return null;
        }

        public override Methods GetMethods(int line, int col, string name)
        {
            return null;
        }

        public override string Goto(VSConstants.VSStd97CmdID cmd, IVsTextView textView, int line, int col, out TextSpan span)
        {
            if (cmd != VSConstants.VSStd97CmdID.GotoDefn && cmd != VSConstants.VSStd97CmdID.GotoDecl)
            {
                span = new TextSpan();
                return null;
            }
            var location = DocumentLocation.Build(_req.FileName, textView, line, col);
            span = location.Span;
            return location.Url;
        }


        private IWpfTextView GetWpfTextView(IVsTextView vTextView)
        {
            if (!(vTextView is IVsUserData userData))
                return null;

            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidViewHost, out object holder);
            var viewHost = (IWpfTextViewHost)holder;

            return viewHost.TextView;
        }

        class DocumentLocation
        {
            private LinkInline _link;
            private string _fileName;

            private DocumentLocation() { }

            public string Url { get; private set; }
            public TextSpan Span { get; private set; }

            public static DocumentLocation Empty()
            {
                return new DocumentLocation();
            }

            public static DocumentLocation Build(string fileName, IVsTextView textView, int line, int col)
            {
                string text = GetTextUpToLine(textView, line);
                MarkdownDocument markdownDocument = MarkdownFactory.ParseToMarkdown(text);
                LinkInline link = FindLinkAtPos(markdownDocument, col, line);
                var location = new DocumentLocation
                {
                    _link = link,
                    _fileName = fileName
                };
                location.Build();
                return location;
            }


            private static string GetTextUpToLine(IVsTextView vstextView, int line)
            {
                vstextView.GetBuffer(out IVsTextLines buffer);
                buffer.GetLengthOfLine(line, out int lineLength);
                buffer.GetLineText(0, 0, line, lineLength, out string text);
                return text;
            }

            private void Build()
            {
                if (_link == null)
                    return;
                if (_link.Url.Contains("://")) // http://, https://, ftp:// ,
                    return;

                string[] linkParts = _link.Url.Split(new[] { '#' }, 2);
                string urlLinkPart = linkParts[0];
                bool isLocalHeadingLink = string.IsNullOrEmpty(urlLinkPart) && linkParts.Length > 1;
                if (isLocalHeadingLink)
                    Span = GetHeadingSpan(linkParts[1]);
                else
                    Url = Path.Combine(Path.GetDirectoryName(_fileName), urlLinkPart);

                // todo add support for links with anchors e.g. [something](some.md#thing)
                // will it require parsing also target file???
            }

            private TextSpan GetHeadingSpan(string headingText)
            {
                return new TextSpan();
                //var headingInline = document.Descendants()
                //    .OfType<HeadingBlock>()
                //    .Select(h=>h.Inline.Descendants<LiteralInline>().FirstOrDefault())
                //    .Where(inline=> inline?.ToString() == headingText)
                //    .FirstOrDefault();
                //if (headingInline == null)
                //    return new TextSpan();
                //return headingInline.ToSourceTextSpan();
            }

            private static LinkInline FindLinkAtPos(MarkdownDocument doc, int col, int? line = null)
            {
                return doc.Descendants().OfType<LinkInline>()
                          .Where(i => i.Line == (line ?? i.Line))
                          .Where(i => i.ToSourceSpan().Contains(col))
                          .FirstOrDefault();
            }


        }
    }
}