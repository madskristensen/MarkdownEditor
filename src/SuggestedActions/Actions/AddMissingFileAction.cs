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
        //private SnapshotSpan errorSpan;
        private string linkUrl;

        public override string DisplayText
        {
            get { return "Create file"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.NewDocument; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            ProjectHelpers.CreateSiblingFile(file, linkUrl);
        }

        public static AddMissingFileAction Create(IEnumerable<IMappingTagSpan<IErrorTag>> errorTags, string file)
        {
            var errorTagMap = errorTags
                .Where(m=>m.Tag.ErrorType == LinkErrorTag.ERROR_TYPE)
                //.Where(m=>m.Tag is LinkErrorTag)
                //.OfType<IMappingTagSpan<LinkErrorTag>>()
                .FirstOrDefault();
            if (errorTagMap == null)
                return null;
            var errorTag = (errorTagMap.Tag as LinkErrorTag);
            var result = new AddMissingFileAction
            {
                //errorSpan = errorTagMap.Span,
                linkUrl = errorTag.Url,
                file = file
            };
            return result;
        }
    }
}
