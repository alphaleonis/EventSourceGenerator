using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alphaleonis.Vsx.Roslyn.CSharp
{
   public static class DocumentExtensions
   {
      public static async Task<CompilationUnitSyntax> GetCompilationUnitRootAsync(this Document document, CancellationToken cancellationToken = default(CancellationToken))
      {
         CompilationUnitSyntax compilationUnit = await document.GetSyntaxRootAsync(cancellationToken) as CompilationUnitSyntax;
         if (compilationUnit == null)
            throw new InvalidOperationException($"The document {document.Name} does not have a C# compilation unit root.");

         return compilationUnit;
      }
   }
}
