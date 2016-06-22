using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    public class MarkdownColorizer : Colorizer
    {
        public MarkdownColorizer(LanguageService svc, IVsTextLines buffer, IScanner scanner) :
            base(svc, buffer, scanner)
        { }
    }
}
