using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sln2CMake
{
    class Debug
    {
        public static void OutputBuildMessage(IServiceProvider serviceProvider, string buildMessage)
        {
            EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)serviceProvider.GetService(typeof(EnvDTE.DTE));

            EnvDTE.OutputWindowPanes panes = dte.ToolWindows.OutputWindow.OutputWindowPanes;
            foreach (EnvDTE.OutputWindowPane pane in panes)
            {
                if (pane.Name.Contains("Build"))
                {
                    pane.OutputString(buildMessage + "\n");
                    pane.Activate();
                    break;
                }
            }
        }

    }
}
