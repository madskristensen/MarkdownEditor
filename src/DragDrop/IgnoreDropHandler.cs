using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using System.Text.RegularExpressions;
using System.Globalization;

namespace MarkdownEditor
{
    internal class MarkdownDropHandler : IDropHandler
    {
        private IWpfTextView _view;
        private string _draggedFileName;
        private string _documentFileName;

        const string _markdownTemplate = "![{0}]({1})";
        static readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".bmp", ".png", ".gif", ".svg", ".tif", ".tiff" };

        public MarkdownDropHandler(IWpfTextView view, string fileName)
        {
            _view = view;
            _documentFileName = fileName;
        }

        public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
        {
            var position = dragDropInfo.VirtualBufferPosition.Position;
            string relative = PackageUtilities.MakeRelative(_documentFileName, _draggedFileName)
                                          .Replace("\\", "/");

            string altText = PrettifyAltText(_draggedFileName);
            string image = string.Format(_markdownTemplate, altText, relative);

            using (var edit = _view.TextBuffer.CreateEdit())
            {
                edit.Insert(position, image);
                edit.Apply();
            }

            return DragDropPointerEffects.Copy;
        }

        private static string PrettifyAltText(string fileName)
        {
            var text = Path.GetFileNameWithoutExtension(fileName)
                            .Replace("-", " ")
                            .Replace("_", " ");

            text = Regex.Replace(text, "(\\B[A-Z])", " $1");

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }


        public void HandleDragCanceled()
        { }

        public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo)
        {
            return DragDropPointerEffects.All;
        }

        public bool IsDropEnabled(DragDropInfo dragDropInfo)
        {
            _draggedFileName = GetImageFilename(dragDropInfo);
            string ext = Path.GetExtension(_draggedFileName);

            if (!_imageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return false;

            return File.Exists(_draggedFileName) || Directory.Exists(_draggedFileName);
        }

        private static string GetImageFilename(DragDropInfo info)
        {
            var data = new DataObject(info.Data);

            if (info.Data.GetDataPresent("FileDrop"))
            {
                // The drag and drop operation came from the file system
                var files = data.GetFileDropList();

                if (files != null && files.Count == 1)
                {
                    return files[0];
                }
            }
            else if (info.Data.GetDataPresent("CF_VSSTGPROJECTITEMS"))
            {
                // The drag and drop operation came from the VS solution explorer
                return data.GetText();
            }

            return null;
        }
    }
}