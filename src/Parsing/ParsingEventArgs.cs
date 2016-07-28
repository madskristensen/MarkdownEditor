using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig.Syntax;
using Microsoft.VisualStudio.Text;

namespace MarkdownEditor.Parsing
{
    public class ParsingEventArgs : EventArgs
    {
        public ParsingEventArgs(MarkdownDocument document, string file, ITextSnapshot snapshot)
        {
            Document = document;
            File = file;
            Snapshot = snapshot;
        }

        public MarkdownDocument Document { get; set; }
        public string File { get; set; }
        public ITextSnapshot Snapshot { get; set; }
    }
}
