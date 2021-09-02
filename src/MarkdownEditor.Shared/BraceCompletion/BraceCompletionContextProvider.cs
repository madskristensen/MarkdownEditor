using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using System.Collections.Generic;

namespace MarkdownEditor
{
    [Export(typeof(IBraceCompletionContextProvider))]
    [BracePair('(', ')')]
    [BracePair('[', ']')]
    [BracePair('{', '}')]
    [BracePair('*', '*')]
    [BracePair('_', '_')]
    [ContentType(MarkdownLanguage.LanguageName)]
    internal sealed class BraceCompletionContextProvider : IBraceCompletionContextProvider
    {
        [Import]
        IClassifierAggregatorService ClassifierService { get; set; }

        public bool TryCreateContext(ITextView textView, SnapshotPoint openingPoint, char openingBrace, char closingBrace, out IBraceCompletionContext context)
        {
            if (IsValidBraceCompletionContext(openingPoint))
            {
                context = new BraceCompletionContext();
                return true;
            }
            else
            {
                context = null;
                return false;
            }
        }

        private bool IsValidBraceCompletionContext(SnapshotPoint openingPoint)
        {
            Debug.Assert(openingPoint.Position >= 0, "SnapshotPoint.Position should always be zero or positive.");

            if (openingPoint.Position > 0)
            {
                IList<ClassificationSpan> classificationSpans = ClassifierService.GetClassifier(openingPoint.Snapshot.TextBuffer)
                                                           .GetClassificationSpans(new SnapshotSpan(openingPoint - 1, 1));

                foreach (ClassificationSpan span in classificationSpans)
                {
                    if (span.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Comment))
                    {
                        return false;
                    }
                    if (span.ClassificationType.IsOfType(PredefinedClassificationTypeNames.String))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}