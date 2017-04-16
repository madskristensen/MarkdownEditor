using MarkdownEditor.Parsing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;

namespace MarkdownEditor
{
    public class MarkdownValidationTagger : ITagger<IErrorTag>
    {
        private ITextBuffer _buffer;
        private string _file;
        private IEnumerable<Error> _errors;
        private List<Error> _errorsCached;  // Cache errors to avoid validation on GetTags

        public MarkdownValidationTagger(ITextBuffer buffer, string file)
        {
            _buffer = buffer;
            _file = file;

            MarkdownFactory.Parsed += MarkdownParsed;

            // Init parsing
            var doc = _buffer.CurrentSnapshot.ParseToMarkdown(_file);
            _errors = doc.Validate(_file);
        }

        private void MarkdownParsed(object sender, ParsingEventArgs e)
        {
            if (string.IsNullOrEmpty(e.File) || e.Snapshot != _buffer.CurrentSnapshot)
                return;

            // Clear cache if document is updated
            _errorsCached = null;
            _errors = e.Document.Validate(e.File);

            SnapshotSpan span = new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || _errors == null || !MarkdownEditorPackage.Options.EnableValidation)
                yield break;

            // Check cache
            var isCached = _errorsCached != null;
            var errors = isCached ? _errorsCached : _errors;
            if (!isCached) _errorsCached = new List<Error>();

            foreach (var error in errors)
            {
                if (!isCached) _errorsCached.Add(error);
                var errorTag = GenerateTag(error);

                if (errorTag != null)
                    yield return errorTag;
            }
        }

        private TagSpan<IErrorTag> GenerateTag(Error error)
        {
            if (_buffer.CurrentSnapshot.Length >= error.Span.End)
            {
                var span = new SnapshotSpan(_buffer.CurrentSnapshot, error.Span);
                return new TagSpan<IErrorTag>(span, error.CreateTag());
            }

            return null;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
