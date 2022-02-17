using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Alphaleonis.Vsx
{
   /// <summary>
   /// Base class for a single file code generator.
   /// </summary>
   [ComVisible(true)]
   public abstract class BaseCodeGenerator : IVsSingleFileGenerator, IObjectWithSite
   {
      #region Private Fields

      private IVsGeneratorProgress m_progress;
      private string m_codeFileNamespace = String.Empty;
      private string m_codeFilePath = String.Empty;
      private ServiceProvider m_serviceProvider;

      #endregion

      #region IVsSingleFileGenerator Members

      /// <summary>
      /// Implements the IVsSingleFileGenerator.DefaultExtension method. 
      /// Returns the extension of the generated file
      /// </summary>
      /// <param name="pbstrDefaultExtension">Out parameter, will hold the extension that is to be given to the output file name. The returned extension must include a leading period</param>
      /// <returns>S_OK if successful, E_FAIL if not</returns>
      int IVsSingleFileGenerator.DefaultExtension(out string pbstrDefaultExtension)
      {
         try
         {
            pbstrDefaultExtension = GetDefaultExtension();
            return VSConstants.S_OK;
         }
         catch (Exception)
         {
            pbstrDefaultExtension = string.Empty;
            return VSConstants.E_FAIL;
         }
      }

      /// <summary>
      /// Implements the IVsSingleFileGenerator.Generate method.
      /// Executes the transformation and returns the newly generated output file, whenever a custom tool is loaded, or the input file is saved
      /// </summary>
      /// <param name="wszInputFilePath">The full path of the input file. May be a null reference (Nothing in Visual Basic) in future releases of Visual Studio, so generators should not rely on this value</param>
      /// <param name="bstrInputFileContents">The contents of the input file. This is either a UNICODE BSTR (if the input file is text) or a binary BSTR (if the input file is binary). If the input file is a text file, the project system automatically converts the BSTR to UNICODE</param>
      /// <param name="wszDefaultNamespace">This parameter is meaningful only for custom tools that generate code. It represents the namespace into which the generated code will be placed. If the parameter is not a null reference (Nothing in Visual Basic) and not empty, the custom tool can use the following syntax to enclose the generated code</param>
      /// <param name="rgbOutputFileContents">[out] Returns an array of bytes to be written to the generated file. You must include UNICODE or UTF-8 signature bytes in the returned byte array, as this is a raw stream. The memory for rgbOutputFileContents must be allocated using the .NET Framework call, System.Runtime.InteropServices.AllocCoTaskMem, or the equivalent Win32 system call, CoTaskMemAlloc. The project system is responsible for freeing this memory</param>
      /// <param name="pcbOutput">[out] Returns the count of bytes in the rgbOutputFileContent array</param>
      /// <param name="pGenerateProgress">A reference to the IVsGeneratorProgress interface through which the generator can report its progress to the project system</param>
      /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns E_FAIL</returns>
      int IVsSingleFileGenerator.Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace, IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
      {
         if (bstrInputFileContents == null)
         {
            throw new ArgumentNullException(bstrInputFileContents);
         }

         m_codeFilePath = wszInputFilePath;
         m_codeFileNamespace = wszDefaultNamespace;
         m_progress = pGenerateProgress;

         byte[] bytes = GenerateCode(wszInputFilePath, bstrInputFileContents);

         if (bytes == null)
         {
            // This signals that GenerateCode() has failed. Tasklist items have been put up in GenerateCode()
            rgbOutputFileContents = null;
            pcbOutput = 0;

            // Return E_FAIL to inform Visual Studio that the generator has failed (so that no file gets generated)
            return VSConstants.E_FAIL;
         }
         else
         {
            // The contract between IVsSingleFileGenerator implementors and consumers is that 
            // any output returned from IVsSingleFileGenerator.Generate() is returned through  
            // memory allocated via CoTaskMemAlloc(). Therefore, we have to convert the 
            // byte[] array returned from GenerateCode() into an unmanaged blob.  

            int outputLength = bytes.Length;
            rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(outputLength);
            Marshal.Copy(bytes, 0, rgbOutputFileContents[0], outputLength);
            pcbOutput = (uint)outputLength;
            return VSConstants.S_OK;
         }
      }

      #endregion

      #region Properties

      /// <summary>
      /// Namespace for the file
      /// </summary>
      protected string FileNamespace
      {
         get
         {
            return m_codeFileNamespace;
         }
      }

      /// <summary>
      /// File-path for the input file
      /// </summary>
      protected string InputFilePath
      {
         get
         {
            return m_codeFilePath;
         }
      }

      /// <summary>
      /// Interface to the VS shell object we use to tell our progress while we are generating
      /// </summary>
      internal IVsGeneratorProgress Progress
      {
         get
         {
            return m_progress;
         }
      }

      #endregion

      #region Abstract Methods

      /// <summary>
      /// Gets the default extension for this generator
      /// </summary>
      /// <returns>String with the default extension for this generator</returns>
      protected abstract string GetDefaultExtension();

      /// <summary>
      /// The method that does the actual work of generating code given the input file
      /// </summary>
      /// <param name="inputFileContent">File contents as a string</param>
      /// <returns>The generated code file as a byte-array</returns>
      protected abstract byte[] GenerateCode(string inputFilePath, string inputFileContent);

      #endregion

      #region Protected Methods

      /// <summary>Method that will communicate an error via the shell callback mechanism.</summary>
      /// <param name="message">Text displayed to the user.</param>
      /// <param name="line">Line number of error.</param>
      /// <param name="column">Column number of error.</param>
      protected virtual void ReportError(string message, int line, int column)
      {
         ThreadHelper.ThrowIfNotOnUIThread();
         IVsGeneratorProgress progress = Progress;
         if (progress != null)
         {
            progress.GeneratorError(0, 0, message, (uint)line, (uint)column);
         }
      }

      protected virtual void ReportError(string message, object location)
      {
         IVsGeneratorProgress progress = Progress;
         ThreadHelper.ThrowIfNotOnUIThread();
         EnvDTE.CodeElement codeElement = location as EnvDTE.CodeElement;
         if (progress != null)
         {
            var startPoint = codeElement == null ? null : ((EnvDTE.CodeElement)location).StartPoint;
            progress.GeneratorError(0, 0, message, startPoint == null ? (uint)0xFFFFFFFF : (uint)startPoint.Line, startPoint == null ? (uint)0xFFFFFFFF : (uint)startPoint.DisplayColumn);
         }
      }

      protected virtual void ReportWarning(string message, object location)
      {
         IVsGeneratorProgress progress = Progress;
         ThreadHelper.ThrowIfNotOnUIThread();
         EnvDTE.CodeElement codeElement = location as EnvDTE.CodeElement;
         if (progress != null)
         {
            var startPoint = codeElement == null ? null : ((EnvDTE.CodeElement)location).StartPoint;
            progress.GeneratorError(1, 0, message, startPoint == null ? (uint)0xFFFFFFFF : (uint)startPoint.Line, startPoint == null ? (uint)0xFFFFFFFF : (uint)startPoint.DisplayColumn);
         }
      }

      /// <summary>Method that will communicate a warning via the shell callback mechanism.</summary>
      /// <param name="message">Text displayed to the user.</param>
      /// <param name="line">Line number of warning.</param>
      /// <param name="column">Column number of warning.</param>
      protected virtual void ReportWarning(string message, int line, int column)
      {
         ThreadHelper.ThrowIfNotOnUIThread();
         IVsGeneratorProgress progress = Progress;
         if (progress != null)
         {
            progress.GeneratorError(1, 0, message, (uint)line, (uint)column);
         }
      }

      /// <summary>Sets an index that specifies how much of the generation has been completed.</summary>
      /// <param name="current">Index that specifies how much of the generation has been completed. This value can range from zero to <paramref name="total"/>.</param>
      /// <param name="total">The maximum value for <paramref name="current"/>.</param>
      protected virtual void ReportProgress(int current, int total)
      {
         ThreadHelper.ThrowIfNotOnUIThread();
         IVsGeneratorProgress progress = Progress;
         if (progress != null)
         {
            progress.Progress((uint)current, (uint)total);
         }
      }

      #region IObjectWithSite Members

      public object Site { get; private set; }

      void IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppvSite)
      {
         IntPtr pUnk = Marshal.GetIUnknownForObject(Site);
         IntPtr intPointer = IntPtr.Zero;
         Marshal.QueryInterface(pUnk, ref riid, out intPointer);
         ppvSite = intPointer;
      }

      void IObjectWithSite.SetSite(object pUnkSite)
      {
         Site = pUnkSite;
      }

      private ServiceProvider SiteServiceProvider
      {
         get
         {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (m_serviceProvider == null && Site != null)
            {
               m_serviceProvider = new ServiceProvider(Site as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            }

            return m_serviceProvider;
         }
      }

      //protected TService GetService<TRegistration, TService>() where TService : class
      //{
      //   var serviceProvider = Site as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
      //   if (serviceProvider != null)
      //   {
      //      var sp = new Microsoft.VisualStudio.Shell.ServiceProvider(serviceProvider);
      //      return sp.GetService<TRegistration, TService>();
      //   }

      //   return default(TService);
      //}
      

      protected object GetService(Guid serviceGuid)
      {
         ThreadHelper.ThrowIfNotOnUIThread();
         return SiteServiceProvider.GetService(serviceGuid);
      }

      /// <summary>
      /// Method to get a service by its Type
      /// </summary>
      /// <param name="serviceType">Type of service to retrieve</param>
      /// <returns>An object that implements the requested service</returns>
      protected object GetService(Type serviceType)
      {
         ThreadHelper.ThrowIfNotOnUIThread();
         return SiteServiceProvider.GetService(serviceType);
      }

      #endregion

      #endregion
   }
}
