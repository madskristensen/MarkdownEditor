using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System.Linq;
using System.Collections.Generic;

namespace MarkdownEditor
{
    class AddMissingFileAction : BaseSuggestedAction
    {
        private string file;
        private string linkUrl;
        private ITextView view;

        public override string DisplayText
        {
            get { return $"Create file {linkUrl}"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.NewDocument; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            string newFilePath = ProjectHelpers.CreateNewFileBesideExistingFile(linkUrl, file);
            if (newFilePath != null)
            {
                RaiseBufferChange();
                ProjectHelpers.OpenFileInPreviewTab(newFilePath);
            }
        }

        private void RaiseBufferChange()
        {
            //Adding and deleting a char in order to force taggers re-evalution
            string text = " ";
            view.TextBuffer.Insert(0, text);
            view.TextBuffer.Delete(new Span(0, text.Length));
        }

        public static AddMissingFileAction Create(IEnumerable<IMappingTagSpan<IErrorTag>> errorTags, string file, ITextView view)
        {
            var errorTag = errorTags
                .Select(m=> m.Tag as LinkErrorTag)
                .Where(tag=>tag !=null)
                .FirstOrDefault();
            if (errorTag == null)
                return null;
            var result = new AddMissingFileAction
            {
                linkUrl = errorTag.Url,
                file = file,
                view = view
            };
            return result;
        }
    }
}
