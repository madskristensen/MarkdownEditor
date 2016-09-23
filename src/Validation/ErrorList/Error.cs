using Microsoft.VisualStudio.Text;

namespace MarkdownEditor
{
    public class Error
    {
        public string Project { get; set; }
        public string File { get; set; }
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string ErrorCode { get; set; }
        public Span  Span { get; set; }
        public bool Fatal { get; set; }
    }
}
