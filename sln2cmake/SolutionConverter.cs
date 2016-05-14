using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Sln2CMake
{
    internal class SolutionConverter
    {
        static public void Run(IServiceProvider serviceProvider, IVsStatusbar statusbar)
        {
            var dte = (DTE2)serviceProvider.GetService(typeof(DTE));
            var projects = dte.Solution.Projects;
            for (int i = 1, n = projects.Count; i <= n; ++i)
            {
                var project = projects.Item(i);
                System.Console.WriteLine("[{0} {1}", i, project.Name);
            }
        }
    }
}
