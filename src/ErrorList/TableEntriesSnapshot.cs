using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;

namespace MarkdownEditor
{
    class TableEntriesSnapshot : TableEntriesSnapshotBase
    {
        private string _projectName;
        private DTE2 _dte;

        internal TableEntriesSnapshot(string file, IEnumerable<Error> errors)
        {
            _dte = (DTE2)Package.GetGlobalService(typeof(DTE));
            _projectName = _dte.Solution.FindProjectItem(file)?.ContainingProject?.Name;
            Errors = new List<Error>(errors);
            File = file;
        }

        public List<Error> Errors { get; }

        public override int VersionNumber { get; } = 1;

        public override int Count
        {
            get { return Errors.Count; }
        }

        public string File { get; set; }

        public override bool TryGetValue(int index, string columnName, out object content)
        {
            content = null;

            if ((index >= 0) && (index < Errors.Count))
            {
                if (columnName == StandardTableKeyNames.DocumentName)
                {
                    content = File;
                }
                else if (columnName == StandardTableKeyNames.ErrorCategory)
                {
                    content = Vsix.Name;
                }
                else if (columnName == StandardTableKeyNames.ErrorSource)
                {
                    content = Vsix.Name;
                }
                else if (columnName == StandardTableKeyNames.Line)
                {
                    content = Errors[index].Line;
                }
                else if (columnName == StandardTableKeyNames.Column)
                {
                    content = Errors[index].Column;
                }
                else if (columnName == StandardTableKeyNames.Text)
                {
                    content = Errors[index].Message;
                }
                else if (columnName == StandardTableKeyNames.FullText || columnName == StandardTableKeyNames.Text)
                {
                    content = Errors[index].Message;
                }
                else if (columnName == StandardTableKeyNames.ErrorSeverity)
                {
                    content = __VSERRORCATEGORY.EC_WARNING;
                }
                else if (columnName == StandardTableKeyNames.Priority)
                {
                    content = vsTaskPriority.vsTaskPriorityMedium;
                }
                else if (columnName == StandardTableKeyNames.ErrorSource)
                {
                    content = ErrorSource.Other;
                }
                else if (columnName == StandardTableKeyNames.BuildTool)
                {
                    content = Vsix.Name;
                }
                else if (columnName == StandardTableKeyNames.ErrorCode)
                {
                    content = Errors[index].ErrorCode;
                }
                else if (columnName == StandardTableKeyNames.ProjectName)
                {
                    content = _projectName;
                }
            }

            return content != null;
        }
    }
}
