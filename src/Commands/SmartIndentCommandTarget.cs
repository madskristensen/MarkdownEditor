using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MarkdownEditor
{
    internal class SmartIndentCommandTarget : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        public static Regex _regex = new Regex(@"^(([\s]+)?(?<bullet>-|\*|>|\+|([0-9a-z]).)\s(\[ \]|\[x\])?\s?)", RegexOptions.IgnoreCase);

        public SmartIndentCommandTarget(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.RETURN)
        { }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var extend = _view.Caret.ContainingTextViewLine.Extent;
            var text = extend.GetText();
            var match = _regex.Match(text);

            if (!match.Success)
                return false;

            var newline = Environment.NewLine;

            if (string.IsNullOrWhiteSpace(_regex.Replace(text, "")))
            {
                using (var edit = _view.TextBuffer.CreateEdit())
                {
                    edit.Delete(extend);
                    edit.Insert(extend.Start, newline);
                    edit.Apply();
                }

                return true;
            }

            var position = _view.Caret.Position.BufferPosition;

            string insertionText = ParseInput(match);

            // Make 2 separate edits so the auto-insertion of list items can be undone (ctrl-z)
            _view.TextBuffer.Insert(position, newline);
            _view.TextBuffer.Insert(position + newline.Length, insertionText);

            return true;
        }

        private string ParseInput(Match match)
        {
            string text = match.Value;
            var bullet = match.Groups["bullet"].Value;

            if (bullet.EndsWith("."))
            {
                var clean = bullet.TrimEnd('.');

                int number;
                if (int.TryParse(bullet, out number))
                {
                    number += 1;
                    text = text.Replace(bullet, number + ".");
                }
                else if (clean.Length == 1 && clean[0] < 'z')
                {
                    var c = clean[0] + 1;
                    text = text.Replace(bullet, (char)c + ".");
                }
            }

            return text;
        }

        protected override bool IsEnabled()
        {
            return MarkdownEditorPackage.Options.EnableSmartIndent;
        }
    }
}