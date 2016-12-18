using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace MarkdownEditor
{
    class SuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly ITextView _view;
        private string _file;
        private IViewTagAggregatorFactoryService _tagService;

        public SuggestedActionsSource(IViewTagAggregatorFactoryService tagService,ITextView view, string file)
        {
            _tagService = tagService;
            _view = view;
            _file = file;
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                return !_view.Selection.IsEmpty;
            });
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            var span = new SnapshotSpan(_view.Selection.Start.Position, _view.Selection.End.Position);
            var startLine = span.Start.GetContainingLine().Extent;
            var endLine = span.End.GetContainingLine().Extent;

            var selectionStart = _view.Selection.Start.Position.Position;
            var selectionEnd = _view.Selection.End.Position.Position;
            var SelectedSpan = new SnapshotSpan(span.Snapshot, selectionStart, selectionEnd - selectionStart);

            var list = new List<SuggestedActionSet>();

            //AddMissingFile
            var addMissingFileAction = AddMissingFileAction.Create(GetErrorTags(_view, SelectedSpan), _file, _view);
            if (addMissingFileAction != null)
                list.AddRange(CreateActionSet(addMissingFileAction));

            if (!_view.Selection.IsEmpty && startLine == endLine)
            {
                var convertToLink = new ConvertToLinkAction(SelectedSpan, _view);
                var convertToImage = new ConvertToImageAction(SelectedSpan, _file);
                list.AddRange(CreateActionSet(convertToLink, convertToImage));
            }

            // Blocks
            var convertToQuote = new ConvertToQuoteAction(SelectedSpan, _view);
            var convertToCodeBlock = new ConvertToCodeBlockAction(SelectedSpan, _view);
            list.AddRange(CreateActionSet(convertToQuote, convertToCodeBlock));

            // Lists
            var convertToUnorderedList = new ConvertToUnorderedList(SelectedSpan, _view);
            var convertToOrderedList = new ConvertToOrderedList(SelectedSpan, _view);
            var convertToTaskList = new ConvertToTaskList(SelectedSpan, _view);
            list.AddRange(CreateActionSet(convertToUnorderedList, convertToOrderedList, convertToTaskList));
            
            return list;
        }

        private IEnumerable<IMappingTagSpan<IErrorTag>> GetErrorTags(ITextView view, SnapshotSpan span)
        {
            return _tagService.CreateTagAggregator<IErrorTag>(view).GetTags(span);
        }

        public IEnumerable<SuggestedActionSet> CreateActionSet(params BaseSuggestedAction[] actions)
        {
            var enabledActions = actions.Where(action => action.IsEnabled);
            return new[] { new SuggestedActionSet(enabledActions) };
        }

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample provider and doesn't participate in LightBulb telemetry
            telemetryId = Guid.Empty;
            return false;
        }


        public event EventHandler<EventArgs> SuggestedActionsChanged
        {
            add { }
            remove { }
        }
    }
}
