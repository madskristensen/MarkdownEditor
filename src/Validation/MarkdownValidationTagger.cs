﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Markdig.Syntax;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace MarkdownEditor
{
    public class MarkdownValidationTagger : ITagger<IErrorTag>
    {
        private MarkdownDocument _doc;
        private bool _isProcessing;
        private ITextBuffer _buffer;
        private string _file;
        private IEnumerable<Error> _errors;
        private List<Error> _errosBuffered;  // Buffer Errors to avoid validation on GetTags

        public MarkdownValidationTagger(ITextBuffer buffer, string file)
        {
            _buffer = buffer;
            _file = file;

            ParseDocument();
            MarkdownFactory.Parsed += MarkdownParsed;
        }

        private void MarkdownParsed(object sender, ParsingEventArgs e)
        {
            if (string.IsNullOrEmpty(e.File) || e.Snapshot != _buffer.CurrentSnapshot)
                return;

            var errors = e.Document.Validate(e.File);
            var errorCount = errors.Count();

            if (errorCount == 0 && (_errors == null || !_errors.Any()))
                return;

            // Clear buffer if document is updated
            _errosBuffered = null;
            _errors = errors;

            SnapshotSpan span = new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || _errors == null || !MarkdownEditorPackage.Options.EnableValidation)
                yield break;

            // Check buffer
            var isBuffered = _errosBuffered != null;
            var errors = isBuffered ? _errosBuffered : _errors;
            if (!isBuffered) _errosBuffered = new List<Error>();

            foreach (var error in errors)
            {
                if (!isBuffered) _errosBuffered.Add(error);
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

        private async void ParseDocument()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            await Task.Run(() =>
            {
                _doc = _buffer.CurrentSnapshot.ParseToMarkdown(_file);
                _errors = _doc.Validate(_file);

                SnapshotSpan span = new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
                _isProcessing = false;

                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
            });
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
