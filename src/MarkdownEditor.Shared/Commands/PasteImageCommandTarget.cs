using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class PasteImage : CommandTargetBase<VSConstants.VSStd97CmdID>
    {
        private string _format = "![{1}]({0})";
        private static string _lastPath;
        private string _fileName;

        public PasteImage(IVsTextView adapter, IWpfTextView textView, string fileName)
            : base(adapter, textView, VSConstants.VSStd97CmdID.Paste)
        {
            _fileName = fileName;
        }

        protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            IDataObject data = Clipboard.GetDataObject();

            if (data == null) return false;

            var formats = data.GetFormats();

            if (formats == null) return false;

            // This is to check if the image is text copied from PowerPoint etc.
            bool trueBitmap = formats.Any(x => new[] { "DeviceIndependentBitmap", "PNG", "JPG", "System.Drawing.Bitmap" }.Contains(x));
            bool textFormat = formats.Any(x => new[] { "Text", "Rich Text Format" }.Contains(x));
            bool hasBitmap = data.GetDataPresent("System.Drawing.Bitmap") || data.GetDataPresent(DataFormats.FileDrop);

            if (!hasBitmap && !trueBitmap || textFormat)
                return false;

            string existingFile = null;

            try
            {
                if (!GetPastedFileName(data, out existingFile))
                    return true;

                _lastPath = Path.GetDirectoryName(existingFile);

                SaveClipboardImageToFile(data, existingFile);
                UpdateTextBuffer(existingFile, _fileName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            return true;
        }

        private bool GetPastedFileName(IDataObject data, out string fileName)
        {
            string extension = "png";
            fileName = "file";

            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                string fullpath = ((string[])data.GetData(DataFormats.FileDrop))[0];
                fileName = Path.GetFileName(fullpath);
                extension = Path.GetExtension(fileName).TrimStart('.');
            }
            else
            {
                extension = GetMimeType((Bitmap)data.GetData("System.Drawing.Bitmap"));
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.FileName = fileName;
                dialog.DefaultExt = "." + extension;
                dialog.Filter = extension.ToUpperInvariant() + " Files|*." + extension;
                dialog.InitialDirectory = _lastPath ?? Path.GetDirectoryName(_fileName);

                if (dialog.ShowDialog() != DialogResult.OK)
                    return false;

                fileName = dialog.FileName;
            }

            return true;
        }

        private static string GetMimeType(Bitmap bitmap)
        {
            if (bitmap.RawFormat.Guid == ImageFormat.Bmp.Guid)
                return "bmp";
            if (bitmap.RawFormat.Guid == ImageFormat.Emf.Guid)
                return "emf";
            if (bitmap.RawFormat.Guid == ImageFormat.Exif.Guid)
                return "exif";
            if (bitmap.RawFormat.Guid == ImageFormat.Gif.Guid)
                return "gif";
            if (bitmap.RawFormat.Guid == ImageFormat.Icon.Guid)
                return "icon";
            if (bitmap.RawFormat.Guid == ImageFormat.Jpeg.Guid)
                return "jpg";
            if (bitmap.RawFormat.Guid == ImageFormat.Tiff.Guid)
                return "tiff";
            if (bitmap.RawFormat.Guid == ImageFormat.Wmf.Guid)
                return "wmf";

            return "png";
        }

        private void UpdateTextBuffer(string fileName, string relativeTo)
        {
            int position = _view.Caret.Position.BufferPosition.Position;
            string relative = PackageUtilities.MakeRelative(relativeTo, fileName)
                                          .Replace("\\", "/");

            string altText = fileName.ToFriendlyName();
            string image = string.Format(CultureInfo.InvariantCulture, _format, relative, altText);

            using (var edit = _view.TextBuffer.CreateEdit())
            {
                edit.Insert(position, image);
                edit.Apply();
            }
        }

        public void SaveClipboardImageToFile(IDataObject data, string existingFile)
        {
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                string original = ((string[])data.GetData(DataFormats.FileDrop))[0];

                if (File.Exists(original))
                    File.Copy(original, existingFile, true);
            }
            else
            {
                using (Bitmap image = (Bitmap)data.GetData("System.Drawing.Bitmap"))
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(existingFile, GetImageFormat(Path.GetExtension(existingFile)));
                }
            }

            var project = ProjectHelpers.DTE.Solution?.FindProjectItem(_fileName)?.ContainingProject;
            project.AddFileToProject(existingFile);
        }

        public static ImageFormat GetImageFormat(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;

                case ".gif":
                    return ImageFormat.Gif;

                case ".bmp":
                    return ImageFormat.Bmp;

                case ".ico":
                    return ImageFormat.Icon;
            }

            return ImageFormat.Png;
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}