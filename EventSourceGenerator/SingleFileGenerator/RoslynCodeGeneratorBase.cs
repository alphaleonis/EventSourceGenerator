using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Alphaleonis.Vsx
{
   [ComVisible(true)]
   public abstract class RoslynCodeGeneratorBase : BaseTextCodeGenerator
   {
      protected sealed override void GenerateCode(string inputFilePath, string inputFileContent, TextWriter writer)
      {
         ThreadHelper.ThrowIfNotOnUIThread();
         Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
         {
            IComponentModel componentModel = (IComponentModel)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SComponentModel));
            VisualStudioWorkspace workspace = componentModel.GetService<VisualStudioWorkspace>();

            if (workspace == null)
               throw new InvalidOperationException($"Unable to get the service {nameof(VisualStudioWorkspace)} from the host application.");

            Solution solution = workspace.CurrentSolution;
            if (solution == null)
               throw new InvalidOperationException($"No solution found in the current workspace.");
     

            ImmutableArray<DocumentId> matchingDocuments = solution.GetDocumentIdsWithFilePath(inputFilePath);
            DocumentId documentId = null;

            // It's a shame we have to resort to using the EnvDTE API here, but it seems to be the only way to retrieve the 
            // actual project of the item that we are saving. (It is possible to have the same source file in several projects)
            // Also, at the time of writing, it does not seem to be possible to retrieve the target framework version from the Roslyn API. 
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            EnvDTE.ProjectItem dteProjectItem = GetService(typeof(EnvDTE.ProjectItem)) as EnvDTE.ProjectItem;
            if (dteProjectItem == null)
               throw new Exception($"Unable to uniquely determine which project item matches the input file \"{inputFilePath}\". Multiple matches was found and the ProjectItem was not available from EnvDTE.");

            EnvDTE.Project dteProject = dteProjectItem.ContainingProject;
            var dteProjectFullName = dteProject.FullName;
            Project roslynProject = solution.Projects.FirstOrDefault(p => p.FilePath == dteProjectFullName);
            if (roslynProject == null)
               throw new Exception($"Unable to determine which project item matches the input file \"{inputFilePath}\". The project with the path \"{dteProject.FullName}\" could not be located.");
               
            string targetFrameworkMoniker = dteProject.Properties?.Item("TargetFrameworkMoniker")?.Value as string;
            if (targetFrameworkMoniker != null)
            {
               TargetFrameworkName = new FrameworkName(targetFrameworkMoniker);
            }
            else
            {
               TargetFrameworkName = null;
            }

            var detProjectItemFileName = dteProjectItem.FileNames[0];
            documentId = roslynProject.Documents.FirstOrDefault(doc => doc.FilePath == detProjectItemFileName)?.Id;

            if (documentId == null)
               throw new CodeGeneratorException(String.Format("Unable to find a document matching the file path \"{0}\".", inputFilePath));

            Document document = solution.GetDocument(documentId);

            document = await GenerateCodeAsync(document);

            await writer.WriteLineAsync((await document.GetTextAsync()).ToString());
         });
      }

      protected FrameworkName TargetFrameworkName { get; private set; }
      
      protected abstract Task<Document> GenerateCodeAsync(Document inputDocument);
   }
}
