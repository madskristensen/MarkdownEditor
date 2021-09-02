using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.BraceCompletion;

namespace MarkdownEditor
{
    [Export(typeof(IBraceCompletionContext))]
    internal class BraceCompletionContext : IBraceCompletionContext
    {
        public bool AllowOverType(IBraceCompletionSession session)
        {
            return true;
        }

        public void Finish(IBraceCompletionSession session)
        {
        }

        public void OnReturn(IBraceCompletionSession session)
        {
        }

        public void Start(IBraceCompletionSession session)
        {
        }
    }
}