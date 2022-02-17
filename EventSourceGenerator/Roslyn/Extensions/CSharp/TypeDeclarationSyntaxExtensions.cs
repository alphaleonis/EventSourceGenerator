using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Alphaleonis.Vsx.Roslyn.CSharp
{
   public static class TypeDeclarationSyntaxExtensions
   {
      public static bool IsPartial(this TypeDeclarationSyntax node)
      {
         return node.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword));
      }
   }
}
