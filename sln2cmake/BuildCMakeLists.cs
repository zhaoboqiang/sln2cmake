//------------------------------------------------------------------------------
// <copyright file="BuildCMakeLists.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Sln2CMake
{
    internal sealed class BuildCMakeLists
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet = new Guid("69ae4734-f6d8-4af0-bba1-2ee3810ec566");

        private readonly Package package;

        private BuildCMakeLists(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static BuildCMakeLists Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static void Initialize(Package package)
        {
            Instance = new BuildCMakeLists(package);
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            var statusbar = ServiceProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;

            SolutionConverter.Run(this.ServiceProvider, statusbar);
        }
    }
}
