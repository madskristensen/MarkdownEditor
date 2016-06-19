using System;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

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
            if (project.IsKind(ProjectTypes.ASPNET_5, ProjectTypes.SSDT, ProjectTypes.MISC))
                return;

            if (DTE.Solution.FindProjectItem(file) == null)
            {
                ProjectItem item = project.ProjectItems.AddFromFile(file);
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
    }

    public static class ProjectTypes
    {
        public const string ASPNET_5 = "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}";
        public const string WEBSITE_PROJECT = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        public const string UNIVERSAL_APP = "{262852C6-CD72-467D-83FE-5EEB1973A190}";
        public const string NODE_JS = "{9092AA53-FB77-4645-B42D-1CCCA6BD08BD}";
        public const string SSDT = "{00d1a9c2-b5f0-4af3-8072-f6c62b433612}";
        public const string MISC = "{66A2671D-8FB5-11D2-AA7E-00C04F688DDE}";
    }
}
