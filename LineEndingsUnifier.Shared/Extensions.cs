using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;

namespace JakubBielawa.LineEndingsUnifier
{
    public static class Extensions
    {
        public static bool EndsWithAny(this string str, string[] strings)
        {
            foreach (var s in strings)
            {
                if (str.EndsWith(s, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool EqualsAny(this string str, string[] strings)
        {
            foreach (var s in strings)
            {
                if (str.Equals(s, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static List<Project> GetAllProjects(this Solution solution)
        {
            Projects projects = solution.Projects;
            List<Project> list = new List<Project>();
            var item = projects.GetEnumerator();

            while (item.MoveNext())
            {
                if (item.Current is Project project)
                {
                    if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    {
                        AddSolutionFolderProjects(list, project);
                    }
                    else
                    {
                        list.Add(project);
                    }
                }
            }

            return list;
        }

        private static void AddSolutionFolderProjects(List<Project> list, Project solutionFolder)
        {
            var projectItems = solutionFolder.ProjectItems;
            for (var i = 1; i <= projectItems.Count; i++)
            {
                var subProject = projectItems.Item(i).SubProject;
                if (subProject != null)
                {
                    if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    {
                        AddSolutionFolderProjects(list, subProject);
                    }
                    else
                    {
                        list.Add(subProject);
                    }
                }
            }
        }
    }
}
