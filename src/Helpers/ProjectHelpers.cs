using System;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;

namespace MarkdownEditor
{
    public static class ProjectHelpers
    {
        static ProjectHelpers()
        {
            DTE = (DTE2)Package.GetGlobalService(typeof(DTE));
        }

        public static DTE2 DTE { get; }

        public static void AddFileToProject(this Project project, string file)
        {
            if (project.IsKind(ProjectTypes.ASPNET_5, ProjectTypes.SSDT, ProjectTypes.MISC, ProjectTypes.SOLUTION_FOLDER))
                return;

            if (DTE.Solution.FindProjectItem(file) == null)
            {
                ProjectItem item = project.ProjectItems.AddFromFile(file);
            }
        }

        public static void AddNestedFile(string parentFile, string newFile, bool force = false)
        {
            ProjectItem item = DTE.Solution.FindProjectItem(parentFile);

            try
            {
                if (item == null
                    || item.ContainingProject == null
                    || item.ContainingProject.IsKind(ProjectTypes.ASPNET_5))
                    return;

                if (item.ProjectItems == null || item.ContainingProject.IsKind(ProjectTypes.UNIVERSAL_APP))
                {
                    item.ContainingProject.AddFileToProject(newFile);
                }
                else if (DTE.Solution.FindProjectItem(newFile) == null || force)
                {
                    item.ProjectItems.AddFromFile(newFile);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }
        public static string CreateNewFileBesideExistingFile(string newFileName, string existingFileFullPath)
        {
            try
            {
                string fileDir = Path.GetDirectoryName(existingFileFullPath);
                string newFilePath = Path.GetFullPath(Path.Combine(fileDir, newFileName));
                Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                File.WriteAllBytes(newFilePath,new byte[0]);

                Project project = DTE.Solution?.FindProjectItem(existingFileFullPath)?.ContainingProject;
                if (project == null)
                    return null;

                project.AddFileToProject(newFilePath);
                return newFilePath;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return null;
            }
        }

        public static bool DeleteFileFromProject(string file)
        {
            ProjectItem item = DTE.Solution.FindProjectItem(file);

            if (item == null)
                return false;

            try
            {
                item.Delete();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
        }


        public static bool IsKind(this Project project, params string[] kindGuids)
        {
            foreach (var guid in kindGuids)
            {
                if (project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void OpenFileInPreviewTab(string file)
        {
            IVsNewDocumentStateContext newDocumentStateContext = null;
            bool failedToOpen = false;

            try
            {
                var openDoc3 = Package.GetGlobalService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument3;

                Guid reason = VSConstants.NewDocumentStateReason.Navigation;
                newDocumentStateContext = openDoc3.SetNewDocumentState((uint)__VSNEWDOCUMENTSTATE.NDS_Provisional, ref reason);

                DTE.ItemOperations.OpenFile(file);
            }
            catch(COMException ex)
            {
                // Not sure why, but it's failing to open the documents in the preview tab when links are clicked
                // in the HTML view.  All we get is an E_ABORT COM exception.  They do open successfully in their
                // own tab though which we'll do below after restoring the state.
                System.Diagnostics.Debug.WriteLine(ex);
                failedToOpen = true;
            }
            finally
            {
                if (newDocumentStateContext != null)
                    newDocumentStateContext.Restore();

                if (failedToOpen)
                    DTE.ItemOperations.OpenFile(file);
            }
        }

        /// <summary>
        /// Returns either a Project or ProjectItem. Returns null if Solution is Selected
        /// </summary>
        /// <returns></returns>
        public static object GetSelectedItem()
        {
            IntPtr hierarchyPointer, selectionContainerPointer;
            object selectedObject = null;
            IVsMultiItemSelect multiItemSelect;
            uint itemId;

            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));

            try
            {
                monitorSelection.GetCurrentSelection(out hierarchyPointer,
                                                 out itemId,
                                                 out multiItemSelect,
                                                 out selectionContainerPointer);

                IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(
                                                     hierarchyPointer,
                                                     typeof(IVsHierarchy)) as IVsHierarchy;

                if (selectedHierarchy != null)
                {
                    ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out selectedObject));
                }

                Marshal.Release(hierarchyPointer);
                Marshal.Release(selectionContainerPointer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }

            return selectedObject;
        }
    }

    public static class ProjectTypes
    {
        public const string ASPNET_5 = "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}";
        public const string WEBSITE_PROJECT = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        public const string UNIVERSAL_APP = "{262852C6-CD72-467D-83FE-5EEB1973A190}";
        public const string NODE_JS = "{9092AA53-FB77-4645-B42D-1CCCA6BD08BD}";
        public const string SSDT = "{00d1a9c2-b5f0-4af3-8072-f6c62b433612}";
        public const string MISC = "{66A2671D-8FB5-11D2-AA7E-00C04F688DDE}";
        public const string SOLUTION_FOLDER = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
    }
}
