﻿using System;
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
            MarkdownFactory.Parsed += MarkdownParsed;

            _solutionEvents = ProjectHelpers.DTE.Events.SolutionEvents;
            _solutionEvents.AfterClosing += SolutionClosed;
            _solutionEvents.ProjectRemoved += ProjectRemoved;

            _documentEvents = ProjectHelpers.DTE.Events.DocumentEvents;
            _documentEvents.DocumentClosing += DocumentClosing;
        }

        private static void ProjectRemoved(Project Project)
        {
            CleanAllErrors();
        }

        private static void SolutionClosed()
        {
            CleanAllErrors();
        }

        private static void DocumentClosing(Document Document)
        {
            if (Document != null)
                CleanErrors(Document.FullName);
        }

        private static void MarkdownParsed(object sender, ParsingEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.File))
            {
                if (!_providers.ContainsKey(e.File)) return;

                var errors = e.Document.Validate(e.File);
                AddErrors(e.File, errors);
            }
        }

        public static void AddErrors(string file, IEnumerable<Error> errors)
        {
            var provider = GetProvider(file);

            provider.Tasks.Clear();

            bool hasFatal = false;
            foreach (var error in errors)
            {
                var task = CreateTask(error, provider);
                if (error.Fatal)
                {
                    hasFatal = true;
                    Logger.Log(error.Message, true);
                }
                provider.Tasks.Add(task);
            }

            if (hasFatal)
            {
                provider.Show();
            }
            else
            {
                provider.Refresh();
            }
        }

        private static ErrorListProvider GetProvider(string file)
        {
            if (_providers.ContainsKey(file))
                return _providers[file];

            var provider = new ErrorListProvider(_provider);
            _providers.Add(file, provider);

            return provider;
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
                ErrorCategory = error.Fatal ? TaskErrorCategory.Error : TaskErrorCategory.Warning,
                Category = error.Fatal ? TaskCategory.BuildCompile : TaskCategory.Html,
                Document = error.File,
                Priority = error.Fatal ? TaskPriority.High : TaskPriority.Normal,
                Text = $"({Vsix.Name}) {error.Message}",
            };

            var item = ProjectHelpers.DTE.Solution.FindProjectItem(error.File);

            if (item != null && item.ContainingProject != null)
                AddHierarchyItem(task, item.ContainingProject);

            task.Navigate += (s, e) =>
            {
                provider.Navigate(task, new Guid(EnvDTE.Constants.vsViewKindPrimary));

                if (task.Column > 0)
                {
                    var doc = (TextDocument)ProjectHelpers.DTE.ActiveDocument.Object("textdocument");
                    doc.Selection.MoveToLineAndOffset(task.Line, task.Column, false);
                }
            };

            return task;
        }

        const uint DISP_E_MEMBERNOTFOUND = 0x80020003;

        public static void AddHierarchyItem(ErrorTask task, Project project)
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
