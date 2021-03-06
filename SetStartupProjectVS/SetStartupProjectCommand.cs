﻿//------------------------------------------------------------------------------
// <copyright file="SetStartupProjectCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using slnStartupProjectLibrary;

namespace SetStartupProjectVS
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SetStartupProjectCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("3af428fe-38b2-4732-9d78-d49305d966a6");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly VSPackage package;


        /// <summary>
        /// control whether an menu item is displayed or not.
        /// </summary>
        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand command = sender as OleMenuCommand;
            if (command != null)
            {
                command.Visible = false;

                var dte = this.package.GetDTE();
                Array projects = (Array)dte.ActiveSolutionProjects;
                foreach (EnvDTE.Project project in projects)
                {
                    command.Visible = true;
                }
            }
        }
 
        /// <summary>
        /// Initializes a new instance of the <see cref="SetStartupProjectCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SetStartupProjectCommand(VSPackage package)
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
                var menuItem = new OleMenuCommand(this.MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += this.BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SetStartupProjectCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(VSPackage package)
        {
            Instance = new SetStartupProjectCommand(package);
        }

#if DEBUG
        /// <summary>
        /// Print to Output Window
        /// </summary>
        internal void OutputString(string output)
        {
            var outPutPane = this.package.OutputPane;
            outPutPane.OutputString(output);
        }

        /// <summary>
        /// Print to Output Window with Line Ending
        /// </summary>
        internal void OutputStringLine(string output)
        {
            OutputString(output + Environment.NewLine);
        }

        /// <summary>
        /// Clear Output Window
        /// </summary>
        internal void ClearOutout()
        {
            var outPutPane = this.package.OutputPane;
            outPutPane.Clear();
        }

        /// <summary>
        /// Clear Output Window
        /// </summary>
        internal void ActivateOutout()
        {
            var outPutPane = this.package.OutputPane;
            outPutPane.Activate();
        }
#endif

        /// <summary>
        /// get the first active project
        /// </summary>
        /// <returns></returns>
        private EnvDTE.Project GetFirstActiveProjct()
        {
            var dte = this.package.GetDTE();
            Array projects = (Array)dte.ActiveSolutionProjects;
            foreach (EnvDTE.Project project in projects)
            {
                return project;
            }
            return null;
        }

        /// <summary>
        /// check if the target Solution, projects, and opened files are changed
        /// </summary>
        /// <returns></returns>
        private bool IsChanged()
        {
            var dte = this.package.GetDTE();
            var solution = dte.Solution;

#if DEBUG
            OutputStringLine(solution.FullName + " : " + (solution.IsDirty ? "Not Saved" : "Saved"));
#endif
            if (solution.IsDirty)
            {
                return true;
            }

            foreach (EnvDTE.Project project in solution.Projects)
            {
#if DEBUG
                OutputStringLine(project.FullName + " : " + (project.IsDirty ? "Not Saved" : "Saved"));
#endif
                if (project.IsDirty)
                {
                    return true;
                }
            }

            foreach (EnvDTE.Document document in dte.Documents)
            {
#if DEBUG
                OutputStringLine(document.FullName + " : " + (document.Saved ? "Saved" : "Not Saved"));
#endif
                if (!document.Saved)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var dte = this.package.GetDTE();
            var solution = dte.Solution;

#if DEBUG
            this.ClearOutout();
            this.ActivateOutout();
#endif
            const string title = "Set Startup Project";
            if (IsChanged())
            {
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    "Solution or projects are not saved. Please save all before running the command",
                    title,
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var activeProject = GetFirstActiveProjct();
            if (activeProject != null)
            {
#if DEBUG
                OutputStringLine("Setting " + activeProject.FileName + " As a Startup Project");
#endif
                solution.SolutionBuild.StartupProjects = activeProject.FileName;

#if DEBUG
                OutputStringLine("calling SetStartupProject " + activeProject.Name);
#endif
                Parser.SetStartupProject(solution.FullName, activeProject.Name);
            }
        }
    }
}
