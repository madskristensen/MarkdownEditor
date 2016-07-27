using System;
using System.Collections.Generic;
using System.IO;
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

        public MarkdownValidationTagger(ITextBuffer buffer, string file)
        {
            _buffer = buffer;
            _file = file;
            ParseDocument();

            _buffer.Changed += bufferChanged;
        }

        private void bufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ParseDocument();
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || _doc == null || !MarkdownEditorPackage.Options.EnableValidation)
                yield break;

            foreach (var error in _errors)
            {
                yield return GenerateTag(error);
            }
        }

        private bool IsUrlValid(string url)
        {
            if (url.Contains("://") || url.StartsWith("/"))
                return true;

            try
            {
                string currentDir = Path.GetDirectoryName(_file);
                string path = Path.Combine(currentDir, url);

                return File.Exists(path);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return true;
            }
        }

        private TagSpan<IErrorTag> GenerateTag(Error error)
        {
            var span = new SnapshotSpan(_buffer.CurrentSnapshot, error.Span);
            var tag = new ErrorTag("Intellisense", error.Message);
            return new TagSpan<IErrorTag>(span, tag);
        }

        private async void ParseDocument()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            await Task.Run(() =>
            {
                _doc = _buffer.CurrentSnapshot.ParseToMarkdown();
                _errors = _doc.Validate(_file);

                TableDataSource.Instance.AddErrors(_file, _errors);

                SnapshotSpan span = new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length);
                _isProcessing = false;

                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
            });
        }


        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
