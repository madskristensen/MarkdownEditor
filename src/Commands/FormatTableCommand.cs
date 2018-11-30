using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;
using System.Windows;
using EnvDTE;
using Markdig;
using Microsoft.VisualStudio.Shell;
using TableSize = System.Drawing.Size;
using TablePos = System.Drawing.Point;

namespace MarkdownEditor
{
    internal sealed class FormatTableCommand
    {
        

        private readonly Package _package;

        private FormatTableCommand(Package package)
        {
            _package = package;

            var commandService = (OleMenuCommandService)ServiceProvider.GetService(typeof(IMenuCommandService));
            if (commandService != null)
            {
                var cmd = new CommandID(PackageGuids.guidPackageCmdSet, PackageIds.FormatTable);
                var menuItem = new OleMenuCommand(Execute, cmd);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        public static FormatTableCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new FormatTableCommand(package);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            var document = ProjectHelpers.DTE.ActiveDocument;

            if (MarkdownLanguage.LanguageName.Equals(document?.Language, StringComparison.OrdinalIgnoreCase))
            {
                button.Visible = true;

                var selection = (TextSelection)document.Selection;

                if (!selection.IsEmpty)
                    button.Enabled = true;
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            var document = ProjectHelpers.DTE.ActiveDocument;
            var selection = (TextSelection)document.Selection;
            var markdown = selection.Text;

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            // var html = Markdown.ToHtml(markdown, pipeline).Replace("\n", Environment.NewLine);
            var ast = Markdown.Parse(markdown, pipeline);
            string tos = ast.ToString();
            string pt = Markdown.ToPlainText(markdown);
            // TODO: Prettify the HTML

            TableDescriptor tableDesc = new TableDescriptor(markdown);
            StringBuilder newTable;
            System.Drawing.Point p;
            if (tableDesc.isGrid)
                newTable = generateGridTable(tableDesc.size, tableDesc.table);
            else
                newTable = generatePipeTable(tableDesc.size, tableDesc.table);

            // Clipboard.SetText(html);
            try
            {
                ProjectHelpers.DTE.UndoContext.Open("Format table");
                string s = newTable.ToString(); //  "New Formatted table\nSecond line.";
                selection.Text = s;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                ProjectHelpers.DTE.UndoContext.Close();
            }
            ProjectHelpers.DTE.StatusBar.Text = "Table formatted.";
        }


        //private void scanText(string text, out Coord size, out bool isGrid, out Dictionary<Coord, string> table)
        //{
        //    size.row = 0;
        //    size.col = 0;
        //    table = new Dictionary<Coord, string>();

        //    isGrid = false;
        //    string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        //    for (int iline = 0; iline < lines.Length; iline++)
        //    {
        //        string line = lines[iline];
        //        line = line.Trim();
        //        line = line.Trim('|');
        //        if (line == "") continue;

        //        // test if line is separator line
        //        bool isSep = true;
        //        for (int i = 0; i < line.Length; i++)
        //        {
        //            if (line[i] != '-' && line[i] != '|' && line[i] != '+' && !char.IsWhiteSpace(line[i]))
        //                isSep = false;
        //        }

        //        if (isSep)
        //        {
        //            if (!isGrid && line[0] == '+')
        //                isGrid = true;
        //        }

        //        string[] cells = line.Split('|');
        //        if (cells.Length > size.col) size.col = cells.Length;

        //        if (isGrid)
        //        {
        //            if (isSep)
        //            {
        //                if(size.row == 0)
        //                {
        //                    // check if we can use this line as template for the column widths

        //                }
        //                else
        //                    size.row++;
        //            }
        //            else
        //            {
        //                for (int i = 0; i < cells.Length; i++)
        //                {
        //                    Coord coord = new Coord(size.row, i);

        //                    if (!table.TryGetValue(coord, out string cellText))
        //                        cellText = "";
        //                    cellText = cellText + " " + cells[i].Trim();
        //                    table[coord] = cellText;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            if (isSep) continue;
        //            for (int i = 0; i < cells.Length; i++)
        //            {
        //                Coord coord = new Coord(size.row, i);
        //                table[coord] = cells[i].Trim();
        //            }
        //            size.row++;
        //        }
        //    }
        //}

        private StringBuilder generatePipeTable(TableSize size, Dictionary<TablePos, string> table)
        {
            int[] colWidth = calcColumnWidths(size, table);

            StringBuilder sb = new StringBuilder();

            for (int ir = 0; ir < size.Height; ir++)
            {
                for (int ic = 0; ic < size.Width; ic++)
                {
                    TablePos c = new TablePos(ic, ir);
                    if (table.TryGetValue(c, out string text))
                    {
                        sb.Append(text.PadRight(colWidth[ic]));
                        if (ic < size.Width - 1)
                            sb.Append("|");
                    }
                }
                sb.AppendLine();
                if (ir == 0)
                {
                    // append separator line
                    for (int ic = 0; ic < size.Width; ic++)
                    {
                        sb.Append(new string('-', colWidth[ic]));
                        if (ic < size.Width - 1)
                            sb.Append("|");
                    }
                    sb.AppendLine();
                }
            }
            return sb;
        }

        private StringBuilder createGridLine(int[] colWidths)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('+');
            for(int i=0; i< colWidths.Length; i++)
            {
                sb.Append('-', colWidths[i]);
                sb.Append('+');
            }
            return sb;
        }

        private StringBuilder generateGridTable(TableSize size, Dictionary<TablePos, string> table)
        {
            int[] colWidth = calcColumnWidths(size, table);
            for (int i = 0; i < colWidth.Length; i++) colWidth[i] = 120 / colWidth.Length;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(createGridLine(colWidth).ToString());
            for (int ir = 0; ir < size.Height; ir++)
            {
                int maxHeight = 0;
                Dictionary<int, List<string>> wrappedColumnText = new Dictionary<int, List<string>>();
                bool emptyRow = true;
                for (int ic = 0; ic < size.Width; ic++)
                {
                    TablePos c = new TablePos(ic, ir);
                    if (table.TryGetValue(c, out string text))
                    {
                        // clean duplicate whitespace
                        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
                        List<string> wrappedText = WordWrap(text, colWidth[ic]);
                        if (maxHeight < wrappedText.Count)
                            maxHeight = wrappedText.Count;
                        wrappedColumnText[ic] = wrappedText;
                        emptyRow = false;
                    }
                    else
                        wrappedColumnText[ic] = new List<string>();
                }

                if (emptyRow) continue;

                for (int iline = 0; iline < maxHeight; iline++)
                {
                    sb.Append("|");
                    for (int ic = 0; ic < size.Width; ic++)
                    {
                        var sl = wrappedColumnText[ic];
                        if (iline < sl.Count)
                        {
                            sb.Append(sl[iline].PadRight(colWidth[ic]));
                        }
                        else
                        {
                            sb.Append(' ',colWidth[ic]);
                        }
                        sb.Append("|");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine(createGridLine(colWidth).ToString());
            }
            return sb;
        }

        private int[] calcColumnWidths(TableSize size, Dictionary<TablePos, string> table)
        {
            int[] colWidth = new int[size.Width];
            for (int ic = 0; ic < size.Width; ic++)
            {
                colWidth[ic] = 0;
                for (int ir = 0; ir < size.Height; ir++)
                {
                    TablePos c = new TablePos(ic, ir);
                    if (table.TryGetValue(c, out string text))
                    {
                        if (colWidth[ic] < text.Length) colWidth[ic] = text.Length;
                    }
                }
            }
            return colWidth;
        }

        // from here : https://stackoverflow.com/questions/3961278/word-wrap-a-string-in-multiple-lines
        protected const string _newline = "\r\n";

        public static List<string> WordWrap(string the_string, int width)
        {
            int pos, next;
            List<string> sl = new List<string>();

            // Lucidity check
            if (width < 1)
            {
                sl.Add(the_string);
                return sl;
            }

            // Parse each line of text
            for (pos = 0; pos < the_string.Length; pos = next)
            {
                // Find end of line
                int eol = the_string.IndexOf(_newline, pos);

                if (eol == -1)
                    next = eol = the_string.Length;
                else
                    next = eol + _newline.Length;

                // Copy this line of text, breaking into smaller lines as needed
                if (eol > pos)
                {
                    do
                    {
                        int len = eol - pos;

                        if (len > width)
                            len = BreakLine(the_string, pos, width);

                        sl.Add(the_string.Substring(pos, len));

                        // Trim whitespace following break
                        pos += len;

                        while (pos < eol && Char.IsWhiteSpace(the_string[pos]))
                            pos++;

                    } while (eol > pos);
                }
                else sl.Add("");
            }

            return sl;
        }

        /// <summary>
        /// Locates position to break the given line so as to avoid
        /// breaking words.
        /// </summary>
        /// <param name="text">String that contains line of text</param>
        /// <param name="pos">Index where line of text starts</param>
        /// <param name="max">Maximum line length</param>
        /// <returns>The modified line length</returns>
        public static int BreakLine(string text, int pos, int max)
        {
            // Find last whitespace in line
            int i = max - 1;
            while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
                i--;
            if (i < 0)
                return max; // No whitespace found; break at maximum length
                            // Find start of whitespace
            while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
                i--;
            // Return length of text before whitespace
            return i + 1;
        }
    }

    internal class TableDescriptor
    {
        public TableSize size;
        public bool isGrid;
        public Dictionary<TablePos, string> table;
        public int[] colWidthsFromContent;
        public List<int> colWidthsFromRuler;

        public TableDescriptor(string text)
        {
            scanText(text);
            colWidthsFromContent = calcColumnWidthFromContent();
        }

        private void scanText(string text)
        {
            size = new TableSize();
            table = new Dictionary<TablePos, string>();
            isGrid = false;

            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int iline = 0; iline < lines.Length; iline++)
            {
                string line = lines[iline];
                line = line.Trim();
                line = line.Trim('|');
                if (line == "") continue;

                // test if line is separator line
                bool isSep = true;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] != '-' && line[i] != '|' && line[i] != '+' && !char.IsWhiteSpace(line[i]))
                        isSep = false;
                }

                if (isSep)
                {
                    if (!isGrid && line[0] == '+')
                        isGrid = true;
                }

                string[] cells = line.Split('|');
                if (cells.Length > size.Width) size.Width = cells.Length;

                if (isGrid)
                {
                    if (isSep)
                    {
                        if (iline == 0)
                        {
                            // check if we can use this line as template for the column widths
                            colWidthsFromRuler = new List<int>();
                            int ilast = 0;
                            for (int i = 1; i < line.Length; i++)
                            {
                                if (line[i] == '+')
                                {
                                    colWidthsFromRuler.Add(i - ilast - 1);
                                    ilast = i;
                                }
                            }
                        }
                        else
                            size.Height++;
                    }
                    else
                    {
                        for (int i = 0; i < cells.Length; i++)
                        {
                            TablePos coord = new TablePos(i, size.Height);

                            if (!table.TryGetValue(coord, out string cellText))
                                cellText = "";
                            cellText = cellText + " " + cells[i].Trim();
                            table[coord] = cellText;
                        }
                    }
                }
                else
                {
                    if (isSep) continue;
                    for (int i = 0; i < cells.Length; i++)
                    {
                        TablePos coord = new TablePos(i, size.Height);
                        table[coord] = cells[i].Trim();
                    }
                    size.Height++;
                }
            }
        }

        private int[] calcColumnWidthFromContent()
        {
            int[] colWidth = new int[size.Width];
            for (int ic = 0; ic < size.Width; ic++)
            {
                colWidth[ic] = 0;
                for (int ir = 0; ir < size.Height; ir++)
                {
                    TablePos c = new TablePos(ic, ir);
                    if (table.TryGetValue(c, out string text))
                    {
                        if (colWidth[ic] < text.Length) colWidth[ic] = text.Length;
                    }
                }
            }
            return colWidth;
        }
    }
}