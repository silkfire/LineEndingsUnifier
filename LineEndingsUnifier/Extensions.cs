namespace LineEndingsUnifier
{
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio.Shell;

    using System.Collections.Generic;
    using System.Linq;

    public static class Extensions
    {
        public static bool EqualsAny(this string str, string[] strings) => strings.Contains(str);

        public static bool EndsWithAny(this string str, string[] strings) => strings.Any(str.EndsWith);


        public static IReadOnlyList<Project> GetAllProjects(this Solution solution)
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

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
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
