using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphaleonis.Vsx.Roslyn.CSharp
{
   public static class CompilationUnitSyntaxExtensions
   {
      /// <summary>
      /// Gets any non-nested classes from this compilation unit, i.e. any classes either directly under
      /// the compilation unit or within a namespace.
      /// </summary>
      /// <param name="compilationUnit">The compilationUnit to act on.</param>    
      public static IEnumerable<ClassDeclarationSyntax> TopLevelClasses(this CompilationUnitSyntax compilationUnit)
      {
         return compilationUnit.DescendantNodes(node => node.IsKind(SyntaxKind.NamespaceDeclaration) || node.IsKind(SyntaxKind.CompilationUnit)).OfType<ClassDeclarationSyntax>();
      }

      /// <summary>
      /// Gets any non-nested interfaces from this compilation unit, i.e. any interfaces either directly under
      /// the compilation unit or within a namespace.
      /// </summary>
      /// <param name="compilationUnit">The compilationUnit to act on.</param>      
      public static IEnumerable<InterfaceDeclarationSyntax> TopLevelInterfaces(this CompilationUnitSyntax compilationUnit)
      {
         return compilationUnit.DescendantNodes(node => node.IsKind(SyntaxKind.NamespaceDeclaration) || node.IsKind(SyntaxKind.CompilationUnit)).OfType<InterfaceDeclarationSyntax>();
      }

      public static CompilationUnitSyntax WithMembers(this CompilationUnitSyntax compilationUnitSyntax, params MemberDeclarationSyntax[] args)
      {
         return compilationUnitSyntax.WithMembers(SyntaxFactory.List(args));
      }

   }
}
