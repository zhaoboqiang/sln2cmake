using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace Sln2CMake
{
    class ProjectConverter
    {
        static private void ParseProjectItem(ProjectItem projectItem)
        {
            var subItems = projectItem.ProjectItems;
            if (subItems != null)
            {
                ParseProjectItems(subItems);
            }

            var subProject = projectItem.SubProject;
            if (subProject != null)
            {
                ParseProject(subProject);
            }
        }

        static private void ParseProjectItems(ProjectItems projectItems)
        {
            for (int i = 1, n = projectItems.Count; i <= n; ++i)
            {
                var projectItem = projectItems.Item(i);
                ParseProjectItem(projectItem);
            }
        }

        public static void ParseProject(Project project)
        {
            var projectTypeGUID = project.Kind;
            switch (projectTypeGUID) {
            case "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}": // Solution Folder 
                ParseProjectItems(project.ProjectItems);
                break;
            case "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}": // Visual Basic
                break;
            case "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}": // Visual C#
                break;
            case "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}": // Visual C++
                break;
            case "{E6FDF86B-F3D1-11D4-8576-0002A516ECE8}": // Visual J#
                break;
            case "{E24C65DC-7377-472b-9ABA-BC803B73C61A}": // Web Project
                break;
            }
        }

        public static void Run(Project project)
        {
            ParseProject(project);
        }
    }
}
