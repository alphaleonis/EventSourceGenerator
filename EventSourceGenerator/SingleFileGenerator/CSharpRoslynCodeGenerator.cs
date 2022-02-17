using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.Shell;

namespace Alphaleonis.Vsx
{
   [ComVisible(true)]
   public abstract class CSharpRoslynCodeGenerator : RoslynCodeGeneratorBase
   {
      protected sealed override async Task<Document> GenerateCodeAsync(Document inputDocument)
      {
         await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
         ReportProgress(0, 100);         
         CompilationUnitSyntax compilationUnit = await GenerateCompilationUnitAsync(inputDocument);
         SyntaxNode targetNode = Formatter.Format(compilationUnit, inputDocument.Project.Solution.Workspace);
         Document resultDocument = inputDocument.Project.AddDocument(Guid.NewGuid().ToString() + ".tmp.g.cs", targetNode);
         resultDocument = await Simplifier.ReduceAsync(resultDocument);
         await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
         ReportProgress(100, 100);
         return resultDocument;
      }

      protected abstract Task<CompilationUnitSyntax> GenerateCompilationUnitAsync(Document sourceDocument);
   }
}
