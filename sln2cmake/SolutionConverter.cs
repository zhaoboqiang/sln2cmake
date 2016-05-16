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

            uint cookie = 0;
            object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Build;

            // Initialize the progress bar.
            statusbar.Progress(ref cookie, 1, "", 0, 0);
            statusbar.Animation(1, ref icon);

            for (uint i = 1, n = (uint)projects.Count; i <= n; ++i)
            {
                var project = projects.Item(i);
                statusbar.Progress(ref cookie, 1, "", i + 1, n);
                statusbar.SetText(string.Format("Converting {0}", project.Name));

                ProjectConverter.Run(project);
            }

            // Clear the progress bar.
            statusbar.Animation(0, ref icon);
            statusbar.Progress(ref cookie, 0, "", 0, 0);
            statusbar.FreezeOutput(0);
            statusbar.Clear();
        }
    }
}
