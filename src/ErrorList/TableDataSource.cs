using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace MarkdownEditor
{
    class TableDataSource : ITableDataSource
    {
        private static TableDataSource _instance;
        private readonly List<SinkManager> _managers = new List<SinkManager>();
        private static Dictionary<string, TableEntriesSnapshot> _snapshots = new Dictionary<string, TableEntriesSnapshot>();
        private  SolutionEvents _solutionEvents;
        private DocumentEvents _documentEvents;

        [Import]
        private ITableManagerProvider TableManagerProvider { get; set; } = null;

        private TableDataSource()
        {
            var compositionService = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            compositionService.DefaultCompositionService.SatisfyImportsOnce(this);

            var manager = TableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
            manager.AddSource(this, StandardTableColumnDefinitions.DetailsExpander,
                                                   StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.ErrorCategory,
                                                   StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName, StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column);

            _solutionEvents = ProjectHelpers.DTE.Events.SolutionEvents;
            _solutionEvents.AfterClosing += SolutionEvents_AfterClosing;
            _solutionEvents.ProjectRemoved += _solutionEvents_ProjectRemoved;

            _documentEvents = ProjectHelpers.DTE.Events.DocumentEvents;
            _documentEvents.DocumentClosing += _documentEvents_DocumentClosing;
        }

        public static TableDataSource Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TableDataSource();

                return _instance;
            }
        }

        public bool HasErrors
        {
            get { return _snapshots.Any(); }
        }

        #region ITableDataSource members
        public string SourceTypeIdentifier
        {
            get { return StandardTableDataSources.ErrorTableDataSource; }
        }

        public string Identifier
        {
            get { return PackageGuids.guidPackageString; }
        }

        public string DisplayName
        {
            get { return Vsix.Name; }
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            return new SinkManager(this, sink);
        }
        #endregion

        private void _solutionEvents_ProjectRemoved(Project Project)
        {
            Instance.CleanAllErrors();
        }

        private void SolutionEvents_AfterClosing()
        {
            Instance.CleanAllErrors();
        }

        private void _documentEvents_DocumentClosing(Document Document)
        {
            Instance.CleanErrors(Document.FullName);
        }

        public void AddSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Add(manager);
            }
        }

        public void RemoveSinkManager(SinkManager manager)
        {
            // This call can, in theory, happen from any thread so be appropriately thread safe.
            // In practice, it will probably be called only once from the UI thread (by the error list tool window).
            lock (_managers)
            {
                _managers.Remove(manager);
            }
        }

        public void UpdateAllSinks()
        {
            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.UpdateSink(_snapshots.Values);
                }
            }
        }

        public void AddErrors(string file, IEnumerable<Error> errors)
        {
            if (errors == null || !errors.Any())
                return;

            var snapshot = new TableEntriesSnapshot(file, errors);
            _snapshots[file] = snapshot;

            UpdateAllSinks();
        }

        public void CleanErrors(params string[] urls)
        {
            foreach (string url in urls)
            {
                if (_snapshots.ContainsKey(url))
                {
                    _snapshots[url].Dispose();
                    _snapshots.Remove(url);
                }
            }

            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.RemoveSnapshots(urls);
                }
            }

            UpdateAllSinks();
        }

        public void CleanAllErrors()
        {
            foreach (string url in _snapshots.Keys)
            {
                var snapshot = _snapshots[url];
                if (snapshot != null)
                {
                    snapshot.Dispose();
                }
            }

            _snapshots.Clear();

            lock (_managers)
            {
                foreach (var manager in _managers)
                {
                    manager.Clear();
                }
            }

            UpdateAllSinks();
        }
    }
}
