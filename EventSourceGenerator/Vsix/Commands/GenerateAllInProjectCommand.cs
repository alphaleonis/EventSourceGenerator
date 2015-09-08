//------------------------------------------------------------------------------
// <copyright file="GenerateAllInProjectCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Linq;
using System.Text;
namespace Alphaleonis.EventSourceGenerator
{
   //[Command(GenerateAllInProjectCommandPackageGuids.PackageGuidString, "{49bfa245-b771-4ddd-b1da-33ccc14e8e8c}", 0x101, IsSingleton = true)]
   //internal sealed class TestCommand : ICommandExtension
   //{
   //   /// <summary>
   //   /// Initializes a new instance of the <see cref="TestCommand"/> class.
   //   /// </summary>
   //   private readonly IDevEnv m_devEnv;

   //   public TestCommand(IDevEnv devEnv)
   //   {
   //      m_devEnv = devEnv;
   //   }

   //   public string Text
   //   {
   //      get
   //      {
   //         return "My Test Command";
   //      }
   //   }

   //   public void Execute(IMenuCommand command)
   //   {
   //      StringBuilder sb = new StringBuilder();
   //      foreach (IProjectNode node in m_devEnv.SolutionExplorer().SelectedNodes.OfType<IProjectNode>())
   //      {
   //         foreach (var item in node.Nodes.OfType<IItemNode>().Where(n => String.IsNullOrEmpty(n.Properties.Pelle)))
   //            sb.AppendLine(item.DisplayName);
   //      }

   //      m_devEnv.MessageBoxService.ShowInformation("This is the test command executing!\r\n" + sb.ToString());
   //   }

   //   public void QueryStatus(IMenuCommand command)
   //   {
   //      command.Enabled = true;         
   //   }
   //}
   ///// <summary>
   ///// Command handler
   ///// </summary>
   //internal sealed class GenerateAllInProjectCommand
   //{
   //   /// <summary>
   //   /// Command ID.
   //   /// </summary>
   //   public const int CommandId = 0x0100;

   //   /// <summary>
   //   /// Command menu group (command set GUID).
   //   /// </summary>
   //   public static readonly Guid MenuGroup = new Guid("49bfa245-b771-4ddd-b1da-33ccc14e8e8c");

   //   /// <summary>
   //   /// VS Package that provides this command, not null.
   //   /// </summary>
   //   private readonly Package package;

   //   /// <summary>
   //   /// Initializes a new instance of the <see cref="GenerateAllInProjectCommand"/> class.
   //   /// Adds our command handlers for menu (commands must exist in the command table file)
   //   /// </summary>
   //   /// <param name="package">Owner package, not null.</param>
   //   private GenerateAllInProjectCommand(Package package)
   //   {
   //      if (package == null)
   //      {
   //         throw new ArgumentNullException("package");
   //      }

   //      this.package = package;

   //      OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
   //      if (commandService != null)
   //      {
   //         CommandID menuCommandID = new CommandID(MenuGroup, CommandId);
   //         EventHandler eventHandler = this.ShowMessageBox;
   //         MenuCommand menuItem = new MenuCommand(eventHandler, menuCommandID);
   //         commandService.AddCommand(menuItem);
   //      }
   //   }

   //   /// <summary>
   //   /// Gets the instance of the command.
   //   /// </summary>
   //   public static GenerateAllInProjectCommand Instance
   //   {
   //      get;
   //      private set;
   //   }

   //   /// <summary>
   //   /// Gets the service provider from the owner package.
   //   /// </summary>
   //   private IServiceProvider ServiceProvider
   //   {
   //      get
   //      {
   //         return this.package;
   //      }
   //   }

   //   /// <summary>
   //   /// Initializes the singleton instance of the command.
   //   /// </summary>
   //   /// <param name="package">Owner package, not null.</param>
   //   public static void Initialize(Package package)
   //   {
   //      Instance = new GenerateAllInProjectCommand(package);
   //   }

   //   /// <summary>
   //   /// Shows a message box when the menu item is clicked.
   //   /// </summary>
   //   /// <param name="sender">Event sender.</param>
   //   /// <param name="e">Event args.</param>
   //   private void ShowMessageBox(object sender, EventArgs e)
   //   {
   //      // Show a Message Box to prove we were here
   //      IVsUIShell uiShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
   //      Guid clsid = Guid.Empty;
   //      int result;
   //      IVsSolution solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
   //      EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE80.DTE2));
         
   //      Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
   //          0,
   //          ref clsid,
   //          "GenerateAllInProjectCommandPackage",
   //          string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName),
   //          string.Empty,
   //          0,
   //          OLEMSGBUTTON.OLEMSGBUTTON_OK,
   //          OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
   //          OLEMSGICON.OLEMSGICON_INFO,
   //          0,        // false
   //          out result));
   //   }
   //}
}
