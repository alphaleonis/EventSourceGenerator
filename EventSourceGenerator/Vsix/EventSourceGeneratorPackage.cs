﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System.Linq;
using Alphaleonis.Vsx;

namespace Alphaleonis.EventSourceGenerator
{
      
   /// <summary>
   /// This is the class that implements the package exposed by this assembly.
   /// </summary>
   /// <remarks>
   /// <para>
   /// The minimum requirement for a class to be considered a valid package for Visual Studio
   /// is to implement the IVsPackage interface and register itself with the shell.
   /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
   /// to do it: it derives from the Package class that provides the implementation of the 
   /// IVsPackage interface and uses the registration attributes defined in the framework to 
   /// register itself and its components with the shell. These attributes tell the pkgdef creation
   /// utility what data to put into .pkgdef file.
   /// </para>
   /// <para>
   /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
   /// </para>
   /// </remarks>
   [PackageRegistration(UseManagedResourcesOnly = true)]
   [InstalledProductRegistration("#1110", "#1112", "1.0", IconResourceID = 1400)] // Info on this package for Help/About
   [ProvideMenuResource("Menus.ctmenu", 1)]
   [Guid(GenerateAllInProjectCommandPackageGuids.PackageGuidString)]
   [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
   [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
   public sealed class EventSourceGeneratorPackage : Package
   {      
      /// <summary>
      /// Initializes a new instance of the <see cref="GenerateAllInProjectCommand"/> class.
      /// </summary>
      public EventSourceGeneratorPackage()
      {
         // Inside this method you can place any initialization code that does not require 
         // any Visual Studio service because at this point the package object is created but 
         // not sited yet inside Visual Studio environment. The place to do all the other 
         // initialization is the Initialize method.
      }

      #region Package Members

      /// <summary>
      /// Initialization of the package; this method is called right after the package is sited, so this is the place
      /// where you can put all the initialization code that rely on services provided by VisualStudio.
      /// </summary>
      protected override void Initialize()
      {
         //GenerateAllInProjectCommand.Initialize(this);
         //Host.Initialize(this);                  
         IVisualStudio devEnv = Toolkit.Initialize(this);
         base.Initialize();
//         devEnv.DialogService.ShowMessageBox("Hello World!");

      }


      protected override object GetService(Type serviceType)
      {
         var service = base.GetService(serviceType);
         IVsUIShell uiShell = service as IVsUIShell;
         
         var asms = AppDomain.CurrentDomain.GetAssemblies().Where(asm =>
         {
            try
            {
               return asm.GetTypes().Any(t => t.FullName == "Microsoft.VisualStudio.Shell.Interop.IVsUIShell");
            }
            catch
            {
               return false;
            }
         }).ToArray();

         uiShell = service as IVsUIShell;

         return service;
      }
      #endregion
   }
}
