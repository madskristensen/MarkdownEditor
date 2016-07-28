using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markdig.Syntax;

namespace MarkdownEditor.Parsing
{
    public class ParsingEventArgs : EventArgs
    {
        public ParsingEventArgs(MarkdownDocument document, string file)
        {
            Document = document;
            File = file;
        }

        public MarkdownDocument Document { get; set; }
        public string File { get; set; }
    }
}
