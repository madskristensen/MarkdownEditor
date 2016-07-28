using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using MarkdownEditor.Parsing;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownEditor
{
    static class ErrorList
    {
        private static Dictionary<string, ErrorListProvider> _providers = new Dictionary<string, ErrorListProvider>();
        private static IServiceProvider _provider;
        private static SolutionEvents _solutionEvents;
        private static DocumentEvents _documentEvents;

        public static void Initialize(IServiceProvider provider)
        {
            _provider = provider;
            MarkdownFactory.Parsed += MarkdownFactory_Parsed;

            _solutionEvents = ProjectHelpers.DTE.Events.SolutionEvents;
            _solutionEvents.AfterClosing += SolutionEvents_AfterClosing;
            _solutionEvents.ProjectRemoved += _solutionEvents_ProjectRemoved;

            _documentEvents = ProjectHelpers.DTE.Events.DocumentEvents;
            _documentEvents.DocumentClosing += _documentEvents_DocumentClosing;
        }

        private static void _solutionEvents_ProjectRemoved(Project Project)
        {
            CleanAllErrors();
        }

        private static void SolutionEvents_AfterClosing()
        {
            CleanAllErrors();
        }

        private static void _documentEvents_DocumentClosing(Document Document)
        {
            CleanErrors(Document.FullName);
        }

        private static async void MarkdownFactory_Parsed(object sender, ParsingEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.File))
            {
                var errors = e.Document.Validate(e.File);

                if (!_providers.ContainsKey(e.File) && !errors.Any())
                    return;

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                AddErrors(e.File, errors);
            }
        }

        public static void AddErrors(string file, IEnumerable<Error> errors)
        {
            CleanErrors(file);

            ErrorListProvider provider = new ErrorListProvider(_provider);
            provider.SuspendRefresh();

            foreach (var error in errors)
            {
                var task = CreateTask(error, provider);
                provider.Tasks.Add(task);
            }

            provider.ResumeRefresh();
            _providers.Add(file, provider);
        }

        public static void CleanErrors(string file)
        {
            if (_providers.ContainsKey(file))
            {
                _providers[file].Tasks.Clear();
                _providers[file].Dispose();
                _providers.Remove(file);
            }
        }

        public static void CleanAllErrors()
        {
            foreach (string file in _providers.Keys)
            {
                var provider = _providers[file];
                if (provider != null)
                {
                    provider.Tasks.Clear();
                    provider.Dispose();
                }
            }

            _providers.Clear();
        }

        private static ErrorTask CreateTask(Error error, ErrorListProvider provider)
        {
            ErrorTask task = new ErrorTask()
            {
                Line = error.Line + 1,
                Column = error.Column + 1,
                ErrorCategory = TaskErrorCategory.Warning,
                Category = TaskCategory.Html,
                Document = error.File,
                Priority = TaskPriority.Normal,
                Text = $"({Vsix.Name}) {error.Message}",
            };

            EnvDTE.ProjectItem item = ProjectHelpers.DTE.Solution.FindProjectItem(error.File);

            if (item != null && item.ContainingProject != null)
                AddHierarchyItem(task, item.ContainingProject);

            task.Navigate += (s, e) =>
            {
                provider.Navigate(task, new Guid(EnvDTE.Constants.vsViewKindPrimary));

                if (task.Column > 0)
                {
                    var doc = (EnvDTE.TextDocument)ProjectHelpers.DTE.ActiveDocument.Object("textdocument");
                    doc.Selection.MoveToLineAndOffset(task.Line, task.Column, false);
                }
            };

            return task;
        }

        const uint DISP_E_MEMBERNOTFOUND = 0x80020003;

        public static void AddHierarchyItem(ErrorTask task, EnvDTE.Project project)
        {
            IVsHierarchy hierarchyItem = null;
            IVsSolution solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            if (solution != null && project != null)
            {
                int flag = -1;

                try
                {
                    flag = solution.GetProjectOfUniqueName(project.FullName, out hierarchyItem);
                }
                catch (COMException ex)
                {
                    if ((uint)ex.ErrorCode != DISP_E_MEMBERNOTFOUND)
                    {
                        throw;
                    }
                }

                if (0 == flag)
                {
                    task.HierarchyItem = hierarchyItem;
                }
            }
        }
    }
}
