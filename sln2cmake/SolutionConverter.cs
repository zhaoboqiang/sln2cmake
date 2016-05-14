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
            uint cookie = 0;

            var dte = (DTE2)serviceProvider.GetService(typeof(DTE));
            var projects = dte.Solution.Projects;

            // Initialize the progress bar.
            statusbar.Progress(ref cookie, 1, "", 0, 0);

            for (uint i = 1, n = (uint)projects.Count; i <= n; ++i)
            {
                var project = projects.Item(i);
                statusbar.Progress(ref cookie, 1, "", i + 1, n);
                statusbar.SetText(string.Format("Converting {0}", project.Name));
            }

            // Clear the progress bar.
            statusbar.Progress(ref cookie, 0, "", 0, 0);
            statusbar.FreezeOutput(0);
            statusbar.Clear();
        }
    }
}
