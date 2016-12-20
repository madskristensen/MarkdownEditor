using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text;
using MarkdownEditor.Parsing;
using Markdig.Syntax;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Editor;
using Markdig.Syntax.Inlines;
using System.IO;

namespace MarkdownEditor
{
    static class MarkdownObjectExtensions
    {
        /// <summary>
        /// Returns object location in source
        /// </summary>
        public static Span ToSourceSpan(this MarkdownObject obj)
        {
            return new Span(obj.Column, obj.Span.Length);
        }

        /// <summary>
        /// Returns object location in source
        /// </summary>
        public static TextSpan ToSourceTextSpan(this MarkdownObject obj)
        {
            return new TextSpan
            {
                iStartLine = obj.Line,
                iEndLine = obj.Line,
                iStartIndex = obj.Column,
                iEndIndex = obj.Column + obj.Span.Length
            };
        }
    }
}