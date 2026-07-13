namespace LineEndingsUnifier
{
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio.Shell;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    internal static class Extensions
    {
        // Windows filenames are case-insensitive and must be compared culture-invariantly
        // (Ordinal avoids the Turkish-I problem), so file-format/filename matching uses
        // OrdinalIgnoreCase rather than the default culture- and case-sensitive comparison.
        public static bool EqualsAny(this string str, string[] strings) => strings.Contains(str, StringComparer.OrdinalIgnoreCase);

        public static bool EndsWithAny(this string str, string[] strings) => strings.Any(s => str.EndsWith(s, StringComparison.OrdinalIgnoreCase));

        public static ReadOnlyCollection<Project> GetAllProjects(this Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projects = new List<Project>();

            foreach (Project project in solution.Projects)
            {
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    projects.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    projects.Add(project);
                }
            }

            return projects.AsReadOnly();
        }

        private static List<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projects = new List<Project>();

            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;

                if (subProject == null)
                {
                    continue;
                }

                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    projects.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    projects.Add(subProject);
                }
            }

            return projects;
        }
    }
}
