using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace MarkdownEditor
{
    class ConvertToImageAction : BaseSuggestedAction
    {
        private SnapshotSpan _span;
        private const string _format = "![{0}]({1})";
        private string _file;

        public ConvertToImageAction(SnapshotSpan span, string file)
        {
            _span = span;
            _file = file;
        }

        public override string DisplayText
        {
            get { return "Convert To Image"; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.Image; }
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            string fileName;
            string dir = Path.GetDirectoryName(_file);

            if (!TryGetFileName(dir, out fileName))
                return;

            string relative = PackageUtilities.MakeRelative(_file, fileName);

            string text = string.Format(_format, _span.GetText(), relative);

            using (var edit = _span.Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(_span, text);
                edit.Apply();
            }
        }

        private bool TryGetFileName(string initialDirectory, out string fileName)
        {
            fileName = null;

            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = initialDirectory;
                dialog.FileName = "Monikers";
                dialog.Filter = "Image files (*.jpg, *.jpeg, *.gif, *.png) | *.jpg; *.jpeg; *.gif; *.png";

                if (dialog.ShowDialog() != DialogResult.OK)
                    return false;

                fileName = dialog.FileName;
            }

            return true;
        }
    }
}
